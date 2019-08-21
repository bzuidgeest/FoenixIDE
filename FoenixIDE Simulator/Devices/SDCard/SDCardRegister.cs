﻿using DequeNet;
//using FoenixIDE.MemoryLocations;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices.SDCard
{
    public class CH376FileInfo
    {
        public bool open;
        public string path;
        public DirectoryInfo directory;
        public bool enumerateMode;
        public IEnumerator directoryIterator;
        //struct stat statbuf;
        public FileInfo file;
        public FileStream fileStream;
        //std::unique_ptr<LongBuffer> byte_read_request;
        //std::unique_ptr<LongBuffer> byte_seek_request;
        public LongBuffer byte_read_request;
        public LongBuffer byte_seek_request;

        /*
        public CH376FileInfo()
        {
            open = false;
            enumerateMode = false;
            path = "";
            file = null;
            directory = null;
        }*/

        public bool IsDirectory()
        {
            return directory.Exists;
        }

        public bool IsFile()
        {
            return file.Exists;
        }

        public bool Exists()
        {
            return directory.Exists | file.Exists;
        }
    }

    public class LongBuffer
    {
        private List<byte> values;
        private uint numOfBytesNeeded;

        public LongBuffer(uint numOfBytesNeeded)
        {
            this.values = new List<byte>();
            this.numOfBytesNeeded = numOfBytesNeeded;
        }

        public void Write(byte value)
        {
            values.Add(value);
        }

        public bool HasValue()
        {
            return values.Count() == numOfBytesNeeded;
        }

        public uint Value()
        {
            uint v = 0;
            for (int i = values.Count(); i > 0; i--)
            {
                v |= (uint)(values[i] << (i * 8));
            }
            return v;
        }

        public void Reset()
        {
            values.Clear();
        }
    }

    public class SDCardReadEvent : EventArgs
    {
        public int Address { get; }
        public byte Value { get; }

        public SDCardReadEvent(int address, byte value)
        {
            this.Address = address;
            this.Value = value;
        }
    }

    public class SDCardWriteEvent : EventArgs
    {
        public int Address { get; }
        public byte Value { get; }

        public SDCardWriteEvent(int address, byte value)
        {
            this.Address = address;
            this.Value = value;
        }
    }

    public class SDCardRegister : MemoryLocations.MemoryRAM
    {
        public const int SDCARD_DATA = 0;
        public const int SDCARD_CMD = 1;

        private SDCommand currentCommand = 0;
        private SDInterruptState interruptState = 0;
        private Deque<byte> outData;
        private bool mounted = false;

        private CH376FileInfo currentFile;

        //private FileInfo currentFile;
        //private direc

        public event EventHandler<SDCardReadEvent> OnRead;
        public event EventHandler<SDCardWriteEvent> OnWrite;

        public SDCardRegister(int startAddress, int length) : base(startAddress, length)
        {
            outData = new Deque<byte>();
        }

        public override byte ReadByte(int addr)
        {
            byte data = 0;

            if (addr == SDCARD_DATA) 
            {
                
                data = outData.PeekLeft();
                outData.PopLeft();
            }

            if (addr == SDCARD_CMD)
            {
                data = (byte)interruptState;
            }

            OnRead?.Invoke(this, new SDCardReadEvent(addr, data));

            return data;
        }

        public override void WriteByte(int address, byte value)
        {
            data[address] = value;

            OnWrite?.Invoke(this, new SDCardWriteEvent(address, value));

            if (address == SDCARD_CMD)
            {
                currentCommand = 0;
                switch ((SDCommand)value)
                {
                    case SDCommand.CHECK_EXIST:
                        outData.PushRight((byte)SDResponse.CMD_RET_SUCCESS);
                        return;
                    case SDCommand.SET_USB_MODE:
                        currentCommand = (SDCommand)value;
                        return;
                    case SDCommand.GET_STATUS:
                        SetInterrupt(false);
                        outData.PushRight((byte)interruptState);
                        interruptState = SDInterruptState.USB_INT_NONE;
                        return;
                    case SDCommand.DISK_MOUNT:
                        mounted = true;
                        interruptState = SDInterruptState.USB_INT_SUCCESS;
                        SetInterrupt(true);
                        return;
                    case SDCommand.SET_FILE_NAME:
                        currentFile = new CH376FileInfo();
                        currentFile.path = ".";
                        currentCommand = SDCommand.SET_FILE_NAME;
                        return;
                    case SDCommand.FILE_OPEN:
                        {
                            if (currentFile.Exists() == false)
                            {
                                interruptState = (SDInterruptState)0x42;  // ERR_MISS_FILE
                                currentFile.open = false;
                            }
                            else if (currentFile.IsDirectory() == true)
                            {
                                interruptState = (SDInterruptState)0x1d;  // docs say ERR_OPEN_DIR but kernel expects
                                                                          // USB_INT_DISK_READ

                                currentFile.directoryIterator = currentFile.directory.GetFileSystemInfos().GetEnumerator();
                                //currentFile.directory_iterator =
                                //    fs::directory_iterator(currentFile.entry);
                                //auto end = fs::directory_iterator();
                                //if (currentFile.directory_iterator == end)
                                //{
                                //    interruptState = 0x42;  // ERR_MISS_FILE
                                //    currentFile.open = false;
                                //}
                                //else
                                //currentFile.directory = new DirectoryInfo(currentFile.path);
                                currentFile.open = true;
                            }
                            else
                            {
                                currentFile.open = true;
                                //currentFile.file = new FileInfo(currentFile.path); // fopen(currentFile.entry.path().string().c_str(), "r");
                                currentFile.fileStream = currentFile.file.OpenRead();
                                //if (!currentFile.f)
                                //{
                                //    interruptState = 0x42;  // ERR_MISS_FILE
                                //    currentFile.open = false;
                                //    return;
                                //}
                                interruptState = SDInterruptState.USB_INT_SUCCESS;
                                return;
                            }
                            if (currentFile.byte_seek_request != null)
                                currentFile.byte_seek_request.Reset();
                            SetInterrupt(true);
                            return;
                        }
                    case SDCommand.FILE_CLOSE:
                        {
                            currentFile.open = false;
                            currentCommand = SDCommand.FILE_CLOSE;
                            //if (fs::is_regular_file(currentFile.entry))
                            //{
                            //    CHECK(fclose(currentFile.f) == 0);
                            //}
                            return;
                        }
                    case SDCommand.FILE_ENUM_GO:
                        {
                            while (currentFile.enumerateMode = currentFile.directoryIterator.MoveNext())
                            {
                                if (((FileSystemInfo)currentFile.directoryIterator.Current).Name != ".")
                                    break;
                            }

                            interruptState = currentFile.enumerateMode ? SDInterruptState.USB_INT_DISK_READ : (SDInterruptState)0x42;
                            SetInterrupt(true);
                        }
                        break;
                    case SDCommand.RD_USB_DATA0:
                        if (currentFile.open)
                        {
                            if (currentFile.enumerateMode &&
                                currentFile.IsDirectory() &&
                                currentFile.directoryIterator.MoveNext())
                            {
                                PushDirectoryListing();
                                return;
                            }
                            else
                            {
                                StreamFileContents();
                                return;
                            }
                        }
                        break;
                    case SDCommand.GET_FILE_SIZE:
                        currentCommand = SDCommand.GET_FILE_SIZE;
                        break;
                    case SDCommand.BYTE_READ:
                        currentCommand = SDCommand.BYTE_READ;
                        currentFile.byte_read_request = new LongBuffer(2);
                        break;
                    case SDCommand.BYTE_RD_GO:
                        if (currentFile.fileStream.Position < currentFile.file.Length)
                        {
                            interruptState = SDInterruptState.USB_INT_DISK_READ;
                        }
                        else
                        {
                            interruptState = SDInterruptState.USB_INT_SUCCESS;  // done reading
                        }
                        SetInterrupt(true);
                        break;
                    case SDCommand.BYTE_LOCATE:
                        // Seek.
                        currentCommand = SDCommand.BYTE_LOCATE;
                        currentFile.byte_seek_request = new LongBuffer(4);
                        break;
                    default:
                        SystemLog.WriteLine(SystemLog.SeverityCodes.Minor, String.Format("UNHANDLED CH376 COMMAND: {0:X}", value));
                        break;
                }
                return;
            };

            if (address == SDCARD_DATA)
            {
                switch (currentCommand)
                {
                    case SDCommand.SET_USB_MODE:
                        //CHECK_EQ(value, 0x03) << "SET_USB_MODE for invalid mode (" << value << ")";
                        outData.PushRight((byte)SDResponse.CMD_RET_SUCCESS);
                        outData.PushRight(0);  // byte 2;
                        return;
                    case SDCommand.SET_FILE_NAME:
                        if (value == 0)
                        {
                            currentFile.directory = new DirectoryInfo(currentFile.path);
                            currentFile.file = new FileInfo(currentFile.path);
                            currentFile.path = "";
                            currentCommand = 0;
                            return;
                        }
                        if (value == '*')
                        {
                            currentFile.enumerateMode = true;
                            return;
                        }
                        currentFile.path = currentFile.path + (char)value;
                        break;
                    case SDCommand.FILE_CLOSE:
                        // value? "Update or not" ?
                        interruptState = SDInterruptState.USB_INT_SUCCESS;
                        SetInterrupt(true);
                        break;
                    case SDCommand.GET_FILE_SIZE:
                        Push32((uint)new FileInfo(currentFile.directory.FullName).Length);
                        break;
                    case SDCommand.BYTE_READ:
                        currentFile.byte_read_request.Write(value);

                        if (currentFile.byte_read_request.HasValue())
                        {
                            uint bytes = currentFile.byte_read_request.Value();
                            if (currentFile.fileStream.Position + bytes > currentFile.file.Length)
                            {
                                interruptState = SDInterruptState.USB_INT_SUCCESS;  // done reading
                            }
                            else
                            {
                                interruptState = SDInterruptState.USB_INT_DISK_READ;
                            }
                            SetInterrupt(true);
                        }
                        break;
                    case SDCommand.BYTE_LOCATE:
                        currentFile.byte_seek_request.Write(value);
                        if (currentFile.byte_seek_request.HasValue())
                        {
                            uint seek_val = currentFile.byte_seek_request.Value();
                            
                            // Seek end of file?
                            if (seek_val == 0xffffffff || seek_val >= currentFile.file.Length)
                            {
                                seek_val = (uint)currentFile.file.Length;
                            }

                            currentFile.fileStream.Seek(seek_val, SeekOrigin.Begin);

                            interruptState = SDInterruptState.USB_INT_SUCCESS;
                            SetInterrupt(true);
                        }
                        break;
                }
            }
        }

        private void PushDirectoryListing()
        {
            // Expect 32 bytes.
            outData.PushRight(0x20);

            // 8.3 filename, only support the first 11 characters, first dot
            // breaks to extension.
            //int c_num = 0;
            byte c = 0;

            FileSystemInfo info = (FileSystemInfo)currentFile.directoryIterator.Current;
            string name = Path.GetFileNameWithoutExtension(info.Name);
            string extension = "";
            if (info.Extension.Length > 0)
                extension = info.Extension.Substring(1);

            for (int i = 0; i < 8; i++)
            {
                if (i < name.Length)
                {
                    c = (byte)name[i];
                    outData.PushRight(c);
                }
                else
                {
                    outData.PushRight(0);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                
                if (i < extension.Length)
                {
                    c = (byte)extension[i];
                    outData.PushRight(c);
                    
                }
                else
                {
                    outData.PushRight(0);
                }
            }

            if (info.Attributes.HasFlag(FileAttributes.Directory))
                outData.PushRight(0x10);
            else
                outData.PushRight(0);
            
            // 10 bytes reserved.
            for (int i = 0; i < 10; i++)
                outData.PushRight(0);
            
            // TODO create/modify times/dates
            for (int i = 0; i < 4; i++)
                outData.PushRight(0);
            
            // Cluster number not supported
            for (int i = 0; i < 2; i++)
                outData.PushRight(0);

            // File size in bytes, 32 bits.
            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                outData.PushRight(0x00);
                outData.PushRight(0x00);
                outData.PushRight(0x00);
                outData.PushRight(0x00);
            }
            else
                Push32((uint)new FileInfo(info.FullName).Length);

            // No more data after this.
            outData.PushRight(0);
            //if (currentFile.directoryIterator.MoveNext() == false)
            //    currentFile.directoryIterator.Reset();

        }

        private void StreamFileContents()
        {
            int total_read = 0;

            // We will only buffer up 255 bytes at a time, and force the client to ask for
            // more, since they're only reading 255 bytes right now at a time anyways
            // despite asking for 64k.
            uint amount_to_retrieve = Math.Min(currentFile.byte_read_request.Value(), 255);

            do
            {
                // We do blocks of no more than 255 bytes;
                byte[] byte_buffer = new byte[255];
                int read_bytes = currentFile.fileStream.Read(byte_buffer, 0, 255);
                outData.PushRight((byte)read_bytes);
                if (read_bytes == 0)
                {
                    return;
                }

                //std::copy(byte_buffer, byte_buffer + read_bytes,
                //          std::back_inserter(outData));
                foreach (byte b in byte_buffer)
                {
                    outData.PushRight(b);
                }

                total_read += read_bytes;
            } while (currentFile.fileStream.Position < currentFile.file.Length && total_read < amount_to_retrieve);
            // If done, put 0.
            if (currentFile.fileStream.Position == currentFile.file.Length)
                outData.PushRight(0);
        }

        // Helper function to set interrupts
        public void SetInterrupt(bool state)
        {
            if (state)
            {
                byte IRQ1 = FoenixSystem.Current.Memory.ReadByte(MemoryLocations.MemoryMap.INT_PENDING_REG1);
                IRQ1 |= 128;
                FoenixSystem.Current.Memory.WriteByte(MemoryLocations.MemoryMap.INT_PENDING_REG1, IRQ1);
                FoenixSystem.Current.CPU.Pins.IRQ = true;
            }
            else
            {
                byte IRQ1 = FoenixSystem.Current.Memory.ReadByte(MemoryLocations.MemoryMap.INT_PENDING_REG1);
                IRQ1 &= 0b01111111;
                FoenixSystem.Current.Memory.WriteByte(MemoryLocations.MemoryMap.INT_PENDING_REG1, IRQ1);
                FoenixSystem.Current.CPU.Pins.IRQ = false;
            }
        }

        // Helper function to push out an int.
        private void Push32(uint value)
        {
            outData.PushRight((byte)(value >> 24));
            outData.PushRight((byte)((value & 0x00ff0000) >> 16));
            outData.PushRight((byte)((value & 0x0000ff00) >> 8));
            outData.PushRight((byte)((value & 0x000000ff)));
        }
    }
}
