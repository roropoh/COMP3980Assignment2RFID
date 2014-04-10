using System;
using SkyeTek.Devices;
using SkyeTek.Tags;
using SkyeTek.STPv3;
using System.Text;

using System.ComponentModel;
using System.Collections;
using System.IO;

namespace SkyeTek
{
    namespace Readers
    {
        public class ReaderException : Exception
        {
            public ReaderException(string message) : base(message) { }
        }
        /// <summary>
        /// Inventory tag callback
        /// </summary>
        /// <param name="tag">A tag found by the inventory process</param>
        /// <param name="context">Object passed in for caller context</param>
        /// <returns>True to end the inventory process if InventoryTags was called with loop set to true</returns>
        public delegate bool InventoryTagDelegate(Tag tag, object context);

        public abstract class Reader
        {
            protected Device m_device;
            
            #region Constructors
            public Reader() : this(null) { }

            public Reader(Device device)
            {
                if (device == null)
                    throw new ArgumentException("Device cannot be null");

                this.m_device = device;
            }
            #endregion

            #region Properties
            public Device Device
            {
                get { return this.m_device; }
            }

            public virtual string FirmwareVersion
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public virtual string SerialNumber
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public virtual string ProductCode
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public virtual string ReaderName
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }
            #endregion

            public virtual void Close()
            {
                this.m_device.Close();
            }

            public virtual void Open()
            {
                this.m_device.Open();
            }

            /// <summary>
            /// Selects a specified tag in the field.  Returns true if tag is selected.
            /// </summary>
            /// <param name="tag">Reference to a tag.  If tag type is not specified, auto-detect will be used.</param>
            /// <returns>True if a tag was selected, false otherwise</returns>
            public abstract bool SelectTag(ref Tag tag);
            
            /// <summary>
            /// Inventory tags in the readers field. 
            /// </summary>
            /// <param name="tag">This parameter describes the tags to inventory (e.g. by type, by epc, by afi, etc...).</param>
            /// <param name="loop">True indicates the reader should inventory tags until explicitly told to stop</param>
            /// <param name="itd">Delegate to call when a tag is inventoried.  Any long running or computationally intensive work should
            /// be done outside of the this function call so as not to impede the inventory process.</param>
            /// <param name="context">Context object passed through to delegate call</param>
            /// <returns>True if the call to InventoryTags was terminated by a call to the InventoryTagDelegate, false if terminated because of another reason</returns>
            public abstract bool InventoryTags(Tag tag, bool loop, InventoryTagDelegate itd, object context);

            public Tag[] SelectTags(Tag tag)
            {
                ArrayList its = new ArrayList();

                InventoryTagDelegate itd = delegate(Tag it, object ctx)
                {
                    its.Add(it);
                    return true;
                };

                this.InventoryTags(tag, false, itd, null);

                return (Tag[])its.ToArray(typeof(Tag));
            }

            /// <summary>
            /// Reads data from the specified tag
            /// </summary>
            /// <param name="tag">Tag to read data from</param>
            /// <param name="address">Start address of read</param>
            /// <param name="blocks">Number of blocks</param>
            /// <returns>Returns null if the read fails, data otherwise</returns>
            public abstract byte[] ReadTagData(Tag tag, ushort address, ushort blocks);

            /// <summary>
            /// Writes data to the specified tag
            /// </summary>
            /// <param name="tag">Tag to write data to</param>
            /// <param name="data">Data to be written</param>
            /// <param name="address">Start address of write</param>
            /// <param name="blocks">Number of blocks</param>
            /// <returns>True if the operation succeeded, false otherwise</returns>
            public abstract bool WriteTagData(Tag tag, byte[] data, ushort address, ushort blocks);
        }

        public class STPv3Reader : Reader
        {

            private byte[] m_RID = { 0xFF, 0xFF, 0xFF, 0xFF };
            private string m_FirmwareVersion;

            public STPv3Reader(Device device) : base(device)
            {
                this.m_FirmwareVersion = null;
            }

         
            /// <summary>
            /// Reader Serial Number
            /// </summary>
            public override string SerialNumber
            {
                get
                {
                    STPv3Request request = new STPv3Request();

                    request.Command = STPv3Commands.READ_SYSTEM_PARAMETER;
                    request.Address = 0;
                    request.Blocks = 4;
                    request.RID = this.m_RID;
                   
                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();

                    if (response == null)
                        return null;

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                        return null;
                    
                    return BitConverter.ToString(response.Data).Replace("-", "");
                    
                }

            }
            /// <summary>
            /// Reader Firmware Version
            /// </summary>
            public override string FirmwareVersion
            {
                get
                {
                    if (this.m_FirmwareVersion != null)
                        return this.m_FirmwareVersion;

                    STPv3Request request = new STPv3Request();

                    request.Command = STPv3Commands.READ_SYSTEM_PARAMETER;
                    request.Address = 1;
                    request.Blocks = 4;
                    request.RID = this.m_RID;
          
                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();

                    if (response == null)
                        return null;

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                        return null;

                    this.m_FirmwareVersion = BitConverter.ToString(response.Data).Replace("-", "");
                    
                    return this.m_FirmwareVersion;
                }
            }
            /// <summary>
            /// Reader Product Code
            /// </summary>
            public override string ProductCode
            {
                get
                {
                    STPv3Request request = new STPv3Request();

                    request.Command = STPv3Commands.READ_SYSTEM_PARAMETER;
                    request.Address = 3;
                    request.Blocks = 2;
                    request.RID = this.m_RID;

                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();

                    if (response == null)
                        return null;

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                        return null;

                    return BitConverter.ToString(response.Data).Replace("-", "");

                }
            }
            /// <summary>
            /// Reader hardware version
            /// </summary>
            public string HardwareVersion
            {
                get
                {
                    STPv3Request request = new STPv3Request();

                    request.Command = STPv3Commands.READ_SYSTEM_PARAMETER;
                    request.Address = 2;
                    request.Blocks = 4;
                    request.RID = this.m_RID;

                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();

                    if (response == null)
                        return null;

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                        return null;


                    return BitConverter.ToString(response.Data).Replace("-", "");
                    
                }
            }
            /// <summary>
            /// Reader Name
            /// </summary>
            public override string ReaderName
            {
                get
                {
                    STPv3Request request = new STPv3Request();

                    request.Command = STPv3Commands.READ_SYSTEM_PARAMETER;
                    request.Address = 5;
                    request.Blocks = 32;
                    request.RID = this.m_RID;
                
                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();


                    if (response == null)
                        return null;

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                        return null;

                    
                    return Encoding.ASCII.GetString(response.Data).Trim(new char[] { '\0' }); 
                }
                set
                {
                    STPv3Request request = new STPv3Request();
                    byte[] readername = new byte[32];
                    Encoding.ASCII.GetBytes(value).CopyTo(readername, 0);
                    

                    request.Command = STPv3Commands.WRITE_SYSTEM_PARAMETER;
                    request.Address = 5;
                    request.Blocks = 32;
                    request.Data = readername;
                    request.RID = this.m_RID;

                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();

                    if (response == null)
                        throw new IOException("No response received from the reader");

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                         throw new ReaderException(String.Format("Unable to set Reader ID, error 0x#{0:X}", response.ResponseCode));

                }
            }
            /// <summary>
            /// Reader ID
            /// </summary>
            public byte[] ReaderID
            {
                get
                {
                    STPv3Request request = new STPv3Request();

                    request.Command = STPv3Commands.READ_SYSTEM_PARAMETER;
                    request.Address = 4;
                    request.Blocks = 4;
                    request.RID = this.m_RID;
                    
                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();

                    if (response == null)
                        return null;

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                        return null;

                    return response.Data;
                    
                }
                set
                {
                    STPv3Request request = new STPv3Request();
                    request.Command = STPv3Commands.WRITE_SYSTEM_PARAMETER;
                    request.Address = 4;
                    request.Blocks = 4;
                    request.Data = value;
                    request.RID = this.m_RID;
                
                issue:
                    request.Issue(this.m_device);
                    STPv3Response response = request.GetResponse();

                    if (response == null)
                        throw new IOException("No response received from the reader");

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                        goto issue;

                    if (!response.Success)
                        throw new ReaderException(String.Format("Unable to set Reader ID, error 0x#{0:X}", response.ResponseCode));

                    request.Data.CopyTo(this.m_RID, 0);
                }
            }

            public override byte[] ReadTagData(Tag tag, ushort address, ushort blocks)
            {

                STPv3Request request = new STPv3Request();
                request.Tag = tag;
                request.Command = STPv3Commands.READ_TAG;
                request.Address = address;
                request.Blocks = blocks;
                request.RID = this.m_RID;

              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();

                if (response == null)
                    return null;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return null;

                return response.Data;
            }

            public override bool WriteTagData(Tag tag, byte[] data, ushort address, ushort blocks)
            {

                STPv3Request request = new STPv3Request();
                request.Tag = tag;
                request.Command = STPv3Commands.WRITE_TAG;
                request.Data = data;
                request.Address = address;
                request.Blocks = blocks;
                request.RID = this.m_RID;
                
              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();

                if (response == null)
                    return false;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return false;

                return true;
            }
            /// <summary>
            /// Writes a system parameter to the reader.  Address denotes parameter
            /// </summary>
            /// <param name="data">Data to be written</param>
            /// <param name="address">Address to write to</param>
            /// <param name="blocks">Number of blocks to write</param>
            public bool WriteSystemParameter(byte[] data, ushort address, ushort blocks)
            {
                STPv3Request request = new STPv3Request();

                request.Command = STPv3Commands.WRITE_SYSTEM_PARAMETER;
                request.Address = address;
                request.Blocks = blocks;
                request.Data = data;
                request.RID = this.m_RID;

              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();

                if (response == null)
                    return false;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return false;

                if (address == 4) // Reader ID is being set
                    request.Data.CopyTo(this.m_RID, 0);

                return true;
            }

            /// <summary>
            /// Reads stored system parameter from reader.  Address denotes parameter
            /// </summary>
            /// <param name="address">Address to read from</param>
            /// <param name="blocks">Number of blocks to read</param>
            /// <returns>Returns null if read failed, data otherwise</returns>
            public byte[] ReadSystemParameter(ushort address, ushort blocks)
            {
                STPv3Request request = new STPv3Request();

                request.Command = STPv3Commands.READ_SYSTEM_PARAMETER;
                request.Address = address;
                request.Blocks = blocks;
                request.RID = this.m_RID;

              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();

                if (response == null)
                    return null;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return null;

                return response.Data;
            }
              
            /// <summary>
            /// Reads a stored default parameter from the reader.  Address denotes parameter
            /// </summary>
            /// <param name="address">Address to read from</param>
            /// <param name="blocks">Number of blocks to write</param>
            /// <returns>Return null if read failed, data otherwise</returns>
            public byte[] RetrieveDefaultParameter(ushort address, ushort blocks)
            {
                STPv3Request request = new STPv3Request();

                request.Command = STPv3Commands.RETRIEVE_DEFAULT_SYSTEM_PARAMETER;
                request.Address = address;
                request.Blocks = blocks;
                request.RID = this.m_RID;

              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();

                if (response == null)
                    return null;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return null;

                return response.Data;   
                
            }
            /// <summary>
            /// Writes a new default system parameter.  Address denotes parameter
            /// </summary>
            /// <param name="data">Data to write</param>
            /// <param name="address">Address to write to</param>
            /// <param name="blocks">Number of blocks to write</param>           
            public bool StoreDefaultParameter(byte[] data, ushort address, ushort blocks)
            {
                STPv3Request request = new STPv3Request();

                request.Command = STPv3Commands.STORE_DEFAULT_SYSTEM_PARAMETER;
                request.Address = address;
                request.Blocks = blocks;
                request.Data = data;
                request.RID = this.m_RID;

              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();

                if (response == null)
                    return false;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return false;

                return true;
            }

            public override bool SelectTag(ref Tag tag)
            {
                STPv3Request request = new STPv3Request();
                request.Tag = tag;
                request.RID = this.m_RID;
                request.Command = STPv3Commands.SELECT_TAG;
                
              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();

                if (response == null)
                    return false;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return false;

                tag.TID = response.TID;
                if (tag.Type == 0x0000)     
                    tag.Type = response.TagType;

                return true;
                
            }

            public override bool InventoryTags(Tag tag, bool loop, InventoryTagDelegate itd, object context)
            {
                STPv3Response response;
                STPv3Request request = new STPv3Request();
                
                request.Tag = tag;
                //request.RID = reader.ReaderID;
                request.Command = STPv3Commands.SELECT_TAG;
                request.Inventory = true;
                request.Loop = loop;

            issue:
                request.Issue(this.m_device);

                response = request.GetResponse();

                if (response == null)
                    return false;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                while (true)
                {
                    response = request.GetResponse();

                    if (response == null)
                        continue;

                    if (!response.Success)
                        return false;

                    if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_PASS)
                    {
                        Tag iTag = new Tag();
                        iTag.TID = response.TID;
                        iTag.Type = response.TagType;

                        //BTM - If we are simply performing an inventory then we let it run to
                        //completion to prevent later synchronization issues
                        if (itd(iTag, context) && loop)
                        {
                            return true;
                        }
                    }
                }
            }

            /// <summary>
            /// Uploads new firmware to a reader.  Takes a string containing the path to the .shf file
            /// </summary>
            /// <param name="file">Path to .shf file.  String.</param>
            /// <returns></returns>
            public bool UploadFirmware(string file)
            {
                STPv3Request request = new STPv3Request();
                request.RID = this.m_RID;
                request.Command = STPv3Commands.ENTER_BOOTLOAD;

              issue:
                request.Issue(this.m_device);
                STPv3Response response = request.GetResponse();
                
                if (response == null)
                    return false;

                if (response.ResponseCode == STPv3ResponseCode.SELECT_TAG_LOOP_OFF)
                    goto issue;

                if (!response.Success)
                    return false;

                STPv3Request.UploadFirmware(this.m_device, file);
                
                return true;
            }
                        
        }

    }
}

