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
            int deviceIndex = memoryMap[address];
            if (deviceIndex != -1)
            {
                device = DevicesByIndex[deviceIndex];
                deviceStartAddress = address - device.BaseAddress;
            }
            else
            {
                device = null;
                deviceStartAddress = address;
            }
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
            if (memoryMap[address] == -1)
                return memory[address];
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                return device.ReadByte(deviceAddress);
            }
        }

        /// <summary>
        /// Reads a 16-bit word from memory
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public int ReadWord(int address)
        {
            if (memoryMap[address] == -1)
            {
                return memory[address] | (memory[address + 1] << 8);
            }
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                return device.ReadByte(deviceAddress) | (device.ReadByte(deviceAddress + 1) << 8);
            }
        }

        /// <summary>
        /// Reads 3 bytes from memory and builds a 24-bit unsigned integer.
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public int ReadLong(int address)
        {
            if (memoryMap[address] == -1)
            {
                return memory[address]
                   | (memory[address + 1] << 8)
                   | (memory[address + 2] << 16);
            }
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                return device.ReadByte(deviceAddress)
                    | (device.ReadByte(deviceAddress + 1) << 8)
                    | (device.ReadByte(deviceAddress + 2) << 16);
            }
        }

        public virtual void WriteByte(int address, byte value)
        {

            //device.WriteByte(deviceAddress, Value);
            if (memoryMap[address] == -1)
                memory[address] = value;
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                DevicesByIndex[memoryMap[address]].WriteByte(deviceAddress, value);
            }
        }

        public void WriteWord(int address, int value)
        {
            if (memoryMap[address] == -1)
            {
                memory[address] = (byte)(value & 0xff);
                memory[address + 1] = (byte)(value >> 8 & 0xff);
            }
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                device.WriteByte(deviceAddress, (byte)(value & 0xff));
                device.WriteByte(deviceAddress + 1, (byte)(value >> 8 & 0xff));
            }
        }

        public void WriteLong(int address, int value)
        {
            if (memoryMap[address] == -1)
            {
                memory[address] = (byte)(value & 0xff);
                memory[address + 1] = (byte)(value >> 8 & 0xff);
                memory[address + 2] = (byte)(value >> 16 & 0xff);
            }
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                device.WriteByte(deviceAddress, (byte)(value & 0xff));
                device.WriteByte(deviceAddress + 1, (byte)(value >> 8 & 0xff));
                device.WriteByte(deviceAddress + 2, (byte)(value >> 16 & 0xff));
            }
        }

        public int Read(int address, int length)
        {
            if (memoryMap[address] == -1)
            {
                int ret = memory[address];
                if (length >= 2)
                    ret += memory[address + 1] << 8;
                if (length >= 3)
                    ret += memory[address + 2] << 16;
                return ret;
            }
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                int ret = device.ReadByte(deviceAddress);
                if (length >= 2)
                    ret += device.ReadByte(deviceAddress + 1) << 8;
                if (length >= 3)
                    ret += device.ReadByte(deviceAddress + 2) << 16;
                return ret;
            }
        }

        internal void Write(int address, int value, int length)
        {
            if (memoryMap[address] == -1)
            {
                memory[address] = (byte)(value & 0xff);
                if (length >= 2)
                    memory[address + 1] = (byte)(value >> 8 & 0xff);
                if (length >= 3)
                    memory[address + 2] = (byte)(value >> 16 & 0xff);
            }
            else
            {
                GetDeviceAt(address, out IMemoryMappedDevice device, out int deviceAddress);
                if (device == null)
                    throw new Exception("No device at " + address.ToString("X6"));
                device.WriteByte(deviceAddress, (byte)(value & 0xff));
                if (length >= 2)
                    device.WriteByte(deviceAddress + 1, (byte)(value >> 8 & 0xff));
                if (length >= 3)
                    device.WriteByte(deviceAddress + 2, (byte)(value >> 16 & 0xff));
            }
        }

        internal void Copy(int sourceAddress, int destinationAddress, int length)
        {
            Array.Copy(memory, sourceAddress, memory, destinationAddress, length);
        }

        internal void Copy(int sourceAddress, byte[] buffer, int destAddress, int length)
        {
            Array.Copy(memory, sourceAddress, buffer, destAddress, length);
        }

        internal void CopyToMemory(byte[] buffer, int sourceAddress, int destAddress, int length)
        {
            Array.Copy(buffer, sourceAddress, memory, destAddress, length);
        }

    }
}
