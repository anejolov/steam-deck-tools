﻿using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using static PerformanceOverlay.Sensors;

namespace PerformanceOverlay
{
    internal class Sensors : IDisposable
    {
        public abstract class Sensor
        {
            public abstract string? GetValue(Sensors sensors);
        }

        public abstract class ValueSensor : Sensor
        {
            public String? Format { get; set; }
            public float Multiplier { get; set; } = 1.0f;
            public bool IgnoreZero { get; set; }

            protected string? ConvertToString(float value)
            {
                if (value == 0 && IgnoreZero)
                    return null;

                value *= Multiplier;
                return value.ToString(Format, CultureInfo.GetCultureInfo("en-US"));
            }
        }

        public class UserValueSensor : ValueSensor
        {
            public delegate float ValueDelegate();

            public ValueDelegate Value { get; set; }

            public override string? GetValue(Sensors sensors)
            {
                return ConvertToString(Value());
            }
        }

        public class HardwareSensor : ValueSensor
        {
            public string HardwareName { get; set; } = "";
            public HardwareType HardwareType { get; set; }
            public string SensorName { get; set; } = "";
            public SensorType SensorType { get; set; }

            public bool Matches(ISensor sensor)
            {
                return sensor != null &&
                    sensor.Hardware.HardwareType == HardwareType &&
                    sensor.Hardware.Name.StartsWith(HardwareName) &&
                    sensor.SensorType == SensorType &&
                    sensor.Name == SensorName;
            }

            public string? GetValue(ISensor sensor)
            {
                if (!sensor.Value.HasValue)
                    return null;

                return ConvertToString(sensor.Value.Value);
            }

            public override string? GetValue(Sensors sensors)
            {
                foreach (var hwSensor in sensors.AllHardwareSensors)
                {
                    if (Matches(hwSensor))
                    {
                        return GetValue(hwSensor);
                    }
                }
                return null;
            }
        }

        public readonly Dictionary<String, Sensor> AllSensors = new Dictionary<string, Sensor>
        {
            {
                "CPU_%", new HardwareSensor()
                {
                    HardwareType = HardwareType.Cpu,
                    HardwareName = "AMD Custom APU 0405",
                    SensorType = SensorType.Load,
                    SensorName = "CPU Total",
                    Format = "F0"
                }
            },
            {
                "CPU_W", new HardwareSensor()
                {
                    HardwareType = HardwareType.Cpu,
                    HardwareName = "AMD Custom APU 0405",
                    SensorType = SensorType.Power,
                    SensorName = "Package",
                    Format = "F1"
                }
            },
            {
                "CPU_T", new HardwareSensor()
                {
                    HardwareType = HardwareType.Cpu,
                    HardwareName = "AMD Custom APU 0405",
                    SensorType = SensorType.Temperature,
                    SensorName = "Core (Tctl/Tdie)",
                    Format = "F1",
                    IgnoreZero = true
                }
            },
            {
                "MEM_GB", new HardwareSensor()
                {
                    HardwareType = HardwareType.Memory,
                    HardwareName = "Generic Memory",
                    SensorType = SensorType.Data,
                    SensorName = "Memory Used",
                    Format = "F1"
                }
            },
            {
                "MEM_MB", new HardwareSensor()
                {
                    HardwareType = HardwareType.Memory,
                    HardwareName = "Generic Memory",
                    SensorType = SensorType.Data,
                    SensorName = "Memory Used",
                    Format = "F0",
                    Multiplier = 1024
                }
            },
            {
                "GPU_%", new HardwareSensor()
                {
                    HardwareType = HardwareType.GpuAmd,
                    HardwareName = "AMD Custom GPU 0405",
                    SensorType = SensorType.Load,
                    SensorName = "D3D 3D",
                    Format = "F0"
                }
            },
            {
                "GPU_MB", new HardwareSensor()
                {
                    HardwareType = HardwareType.GpuAmd,
                    HardwareName = "AMD Custom GPU 0405",
                    SensorType = SensorType.SmallData,
                    SensorName = "D3D Dedicated Memory Used",
                    Format = "F0"
                }
            },
            {
                "GPU_GB", new HardwareSensor()
                {
                    HardwareType = HardwareType.GpuAmd,
                    HardwareName = "AMD Custom GPU 0405",
                    SensorType = SensorType.SmallData,
                    SensorName = "D3D Dedicated Memory Used",
                    Format = "F0",
                    Multiplier = 1.0f/1024.0f
                }
            },
            {
                "GPU_W", new HardwareSensor()
                {
                    HardwareType = HardwareType.GpuAmd,
                    HardwareName = "AMD Custom GPU 0405",
                    SensorType = SensorType.Power,
                    SensorName = "GPU SoC",
                    Format = "F1"
                }
            },
            {
                "GPU_T", new HardwareSensor()
                {
                    HardwareType = HardwareType.GpuAmd,
                    HardwareName = "AMD Custom GPU 0405",
                    SensorType = SensorType.Temperature,
                    SensorName = "GPU Temperature",
                    Format = "F1",
                    IgnoreZero = true
                }
            },
            {
                "BATT_%", new HardwareSensor()
                {
                    HardwareType = HardwareType.Battery,
                    HardwareName = "GETAC",
                    SensorType = SensorType.Level,
                    SensorName = "Charge Level",
                    Format = "F0"
                }
            },
            {
                "BATT_W", new HardwareSensor()
                {
                    HardwareType = HardwareType.Battery,
                    HardwareName = "GETAC",
                    SensorType = SensorType.Power,
                    SensorName = "Charge/Discharge Rate",
                    Format = "F1"
                }
            },
            {
                "FAN_RPM", new UserValueSensor()
                {
                    Value = delegate ()
                    {
                        return (float)CommonHelpers.Vlv0100.GetFanRPM();
                    },
                    Format = "F0"
                }
            }
        };

        private LibreHardwareMonitor.Hardware.Computer libreHardwareComputer = new LibreHardwareMonitor.Hardware.Computer
        {
            IsCpuEnabled = true,
            IsMemoryEnabled = true,
            IsGpuEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true
        };

        public IList<ISensor> AllHardwareSensors { get; private set; } = new List<ISensor>();

        public Sensors()
        {
            libreHardwareComputer.Open();
        }

        public void Dispose()
        {
            libreHardwareComputer.Close();
        }

        public void Update()
        {
            var allSensors = new List<ISensor>();

            foreach (IHardware hardware in libreHardwareComputer.Hardware)
            {
                try
                {
                    hardware.Update();
                }
                catch (SystemException) { }
                hardware.Accept(new SensorVisitor(sensor => allSensors.Add(sensor)));
            }

            this.AllHardwareSensors = allSensors;
        }
        
        public string? GetValue(String name)
        {
            if (!AllSensors.ContainsKey(name))
                return null;

            return AllSensors[name].GetValue(this);
        }
    }
}