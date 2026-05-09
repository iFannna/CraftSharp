using System;
using System.Diagnostics;
using LibreHardwareMonitor.Hardware;

namespace CraftSharp.Services
{
    /// <summary>
    /// 数据映射服务 - 统一管理所有数据映射逻辑（电池/CPU/内存/GPU）
    /// </summary>
    public class DataMappingService : IDisposable
    {
        private static DataMappingService? _instance;
        public static DataMappingService Instance => _instance ??= new DataMappingService();

        // 性能计数器（缓存以避免每次调用NextValue的延迟）
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _availableMemoryCounter;

        // LibreHardwareMonitor实例（用于GPU利用率）
        private Computer? _computer;
        private ISensor? _gpuLoadSensor;

        private bool _initialized = false;
        private bool _libreHardwareInitialized = false;

        /// <summary>
        /// 初始化服务（初始化性能计数器）
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");

                // 第一次调用NextValue返回0，需要预热
                _cpuCounter.NextValue();
                _availableMemoryCounter.NextValue();
            }
            catch
            {
                // 如果初始化失败，计数器将为null，GetValue将返回默认值
            }

            _initialized = true;
        }

        /// <summary>
        /// 获取数据映射值（百分比 0.0 - 1.0）
        /// </summary>
        public double GetValue(string mappingType)
        {
            switch (mappingType)
            {
                case "电池电量":
                    var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
                    return powerStatus.BatteryLifePercent;

                case "内存占用率":
                    try
                    {
                        if (_availableMemoryCounter != null)
                        {
                            double availableMB = _availableMemoryCounter.NextValue();
                            double totalMB = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024.0);
                            double usedPercent = (totalMB - availableMB) / totalMB;
                            return Math.Min(1.0, Math.Max(0.0, usedPercent));
                        }
                        return 0;
                    }
                    catch
                    {
                        return 0;
                    }

                case "CPU利用率":
                    try
                    {
                        if (_cpuCounter != null)
                        {
                            return Math.Min(1.0, _cpuCounter.NextValue() / 100.0);
                        }
                        return 0;
                    }
                    catch
                    {
                        return 0;
                    }

                case "GPU利用率":
                    try
                    {
                        // 初始化LibreHardwareMonitor（仅第一次）
                        if (!_libreHardwareInitialized)
                        {
                            InitializeLibreHardwareMonitor();
                            _libreHardwareInitialized = true;
                        }

                        if (_gpuLoadSensor != null)
                        {
                            // 更新传感器值
                            _gpuLoadSensor.Hardware.Update();
                            float? gpuLoad = _gpuLoadSensor.Value;
                            if (gpuLoad.HasValue)
                            {
                                return Math.Min(1.0, Math.Max(0.0, gpuLoad.Value / 100.0));
                            }
                        }
                        return 0;
                    }
                    catch
                    {
                        return 0;
                    }

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 初始化LibreHardwareMonitor（用于GPU利用率）
        /// </summary>
        private void InitializeLibreHardwareMonitor()
        {
            try
            {
                _computer = new Computer
                {
                    IsGpuEnabled = true
                };
                _computer.Open();

                // 查找GPU负载传感器
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.GpuNvidia ||
                        hardware.HardwareType == HardwareType.GpuAmd ||
                        hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Core"))
                            {
                                _gpuLoadSensor = sensor;
                                break;
                            }
                        }
                        if (_gpuLoadSensor != null)
                            break;
                    }
                }
            }
            catch
            {
                // 如果初始化失败，_gpuLoadSensor将为null
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _availableMemoryCounter?.Dispose();

            if (_computer != null)
            {
                _computer.Close();
                _computer = null;
            }

            _gpuLoadSensor = null;
            _initialized = false;
            _libreHardwareInitialized = false;
        }
    }
}