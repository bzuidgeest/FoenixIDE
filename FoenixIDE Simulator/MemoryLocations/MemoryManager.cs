using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FoenixIDE.Processor;
using FoenixIDE.Display;
using FoenixIDE.MemoryLocations;
using FoenixIDE.Simulator.FileFormat;
using FoenixIDE.Simulator.Devices;
using FoenixIDE.Simulator.Devices.SDCard;
using System.Linq;

namespace FoenixIDE.MemoryLocations
{
    /// <summary>
    /// Maps an address on the bus to a device or memory. GPU, RAM, and ROM are hard coded. Other I/O devices will be added 
    /// later.
    /// </summary>
    public class MemoryManager
    {

        public const int MinAddress = 0x00_0000;
        public const int MaxAddress = 0xff_ffff;

        private byte[] memory = new byte[0xFFF_ffff];
        private int[] memoryMap = new int[0xFFF_ffff];

        private Dictionary<string, IMemoryMappedDevice> DevicesByName { get; } = new Dictionary<string, IMemoryMappedDevice>();
        private Dictionary<int, IMemoryMappedDevice> DevicesByIndex { get; } = new Dictionary<int, IMemoryMappedDevice>();


        

        public MemoryManager()
        {
            memory.AsSpan().Fill(0);
            memoryMap.AsSpan().Fill(-1);
        }

        /// <summary>
        /// Set a memory based device at a specific location of memory
        /// </summary>
        /// <param name="device">The device to set</param>
        /// <param name="startAddres">The addres the device is based from.</param>
        /// <param name="size">The size (in bytes) of memory the device fills</param>
        public void AddDevice(IMemoryMappedDevice device)
        {
            device.SetMemory(memory.AsMemory(device.BaseAddress, device.Size));
            DevicesByName.Add(device.Name, device);
            int index = 0;
            if (DevicesByIndex.Keys.Count != 0)
             index = DevicesByIndex.Keys.Max() + 1;
            DevicesByIndex.Add(index, device);
            memoryMap.AsSpan(device.BaseAddress, device.Size).Fill(index);
        }

        public void RemoveDevice(IMemoryMappedDevice device)
        {
            memoryMap.AsSpan(device.BaseAddress, device.Size).Fill(-1);
            DevicesByName.Remove(device.Name);
            DevicesByIndex.Remove(DevicesByIndex.Single(x => x.Value == device).Key);
        }

        /// <summary>
        /// Determine whehter the address being read from or written to is an I/O device or a memory cell.
        /// If the location is an I/O device, return that device. Otherwise, return the memory being referenced.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="device"></param>
        /// <param name="deviceStartAddress"></param>
        public void GetDeviceAt(int address, out IMemoryMappedDevice device, out int deviceStartAddress)
        {
            device = DevicesByIndex[memoryMap[address]];
            deviceStartAddress = address - device.BaseAddress;
        }

        public IMemoryMappedDevice GetDeviceByName(string name)
        {
            return DevicesByName[name];
        }

        public virtual byte this[int Address]
        {
            get { return ReadByte(Address); }
            set { WriteByte(Address, value); ; }
        }

        public virtual byte this[int Bank, int Address]
        {
            get { return ReadByte(Bank * 0xffff + Address & 0xffff); }
            set { WriteByte(Bank * 0xffff + Address & 0xffff, value); }
        }

        /// <summary>
        /// Finds device mapped to 'Address' and calls it 
        /// 'Address' is offset by GetDeviceAt to device internal address range
        /// </summary>
        public byte ReadByte(int address)
        {

            GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
            if (memoryMap[address] == -1)
                return memory[address];
            else
                return device.ReadByte(deviceAddress);
        }

        /// <summary>
        /// Reads a 16-bit word from memory
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public int ReadWord(int Address)
        {
            GetDeviceAt(Address, out IMemoryMappedDevice device, out int deviceAddress);
            return device.ReadByte(deviceAddress) | (device.ReadByte(deviceAddress + 1) << 8);
        }

        /// <summary>
        /// Reads 3 bytes from memory and builds a 24-bit unsigned integer.
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public int ReadLong(int Address)
        {
            GetDeviceAt(Address, out IMemoryMappedDevice device, out int deviceAddress);
            return device.ReadByte(deviceAddress)
                | (device.ReadByte(deviceAddress + 1) << 8)
                | (device.ReadByte(deviceAddress + 2) << 16);
        }

        public virtual void WriteByte(int address, byte value)
        {
            GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
            //device.WriteByte(deviceAddress, Value);
            if (memoryMap[address] == -1)
                memory[address] = value;
            else
                DevicesByIndex[memoryMap[address]].WriteByte(deviceAddress, value);
        }

        public void WriteWord(int Address, int Value)
        {
            GetDeviceAt(Address, out IMemoryMappedDevice device, out int deviceAddress);
            device.WriteByte(deviceAddress, (byte)(Value & 0xff));
            device.WriteByte(deviceAddress + 1, (byte)(Value >> 8 & 0xff));
        }

        public void WriteLong(int Address, int Value)
        {
            GetDeviceAt(Address, out IMemoryMappedDevice device, out int deviceAddress);
            device.WriteByte(deviceAddress, (byte)(Value & 0xff));
            device.WriteByte(deviceAddress + 1, (byte)(Value >> 8 & 0xff));
            device.WriteByte(deviceAddress + 2, (byte)(Value >> 16 & 0xff));
        }

        public int Read(int Address, int Length)
        {
            GetDeviceAt(Address, out IMemoryMappedDevice device, out int deviceAddress);
            int addr = deviceAddress;
            int ret = device.ReadByte(addr);
            if (Length >= 2)
                ret += device.ReadByte(addr + 1) << 8;
            if (Length >= 3)
                ret += device.ReadByte(addr + 2) << 16;
            return ret;
        }

        internal void Write(int Address, int Value, int Length)
        {
            GetDeviceAt(Address, out IMemoryMappedDevice device, out int deviceAddress);
            if (device == null)
                throw new Exception("No device at " + Address.ToString("X6"));
            device.WriteByte(deviceAddress, (byte)(Value & 0xff));
            if (Length >= 2)
                device.WriteByte(deviceAddress + 1, (byte)(Value >> 8 & 0xff));
            if (Length >= 3)
                device.WriteByte(deviceAddress + 2, (byte)(Value >> 16 & 0xff));
        }

        internal void Copy(int sourceAddress, int destinationAddress, int length)
        {
            Array.Copy(memory, sourceAddress, memory, destinationAddress, length);
        }

        internal void Copy(int sourceAddress, byte[] buffer, int destAddress, int length)
        {
            Array.Copy(memory, sourceAddress, buffer, destAddress, length);
        }
    }
}
