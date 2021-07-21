using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TomPIT.Plc.Addressing
{
    internal enum S7AddressingMode
    {
        Unknown,
        Input,
        Output,
        PeripheralInput,
        PeripheralOutput,
        FlagMemory,
        DataBlock,
        Timer,
        Counter
    }

    internal enum S7DataType
    {
        /// <summary>
        /// 1 Bit (Logical)
        /// </summary>
        Bit,

        /// <summary>
        /// 8 Bit Signed Integer (-128...127)
        /// </summary>
        SInt,

        /// <summary>
        /// 16 Bit SIgned Integer (-32.768...32.767)
        /// </summary>
        Int,

        /// <summary>
        /// 32 Bit Signed Integer (-2.147.483.648...2.147.483.647)
        /// </summary>
        DInt,

        /// <summary>
        /// 64 Bit Signed Integer (-9.223.372.036.854.775.808...9.223.372.036.854.775.807)
        /// </summary>
        LInt,

        /// <summary>
        /// 8 Bit Unsigned Integer (0...255)
        /// </summary>
        USInt,

        /// <summary>
        /// 16 Bit Unsigned Integer (0...65.535)
        /// </summary>
        UInt,

        /// <summary>
        /// 32 Bit Unsigned Integer (0...4.294.967.295)
        /// </summary>
        UDInt,

        /// <summary>
        /// 64 Bit Unsigned Integer (0...18.446.744.073.709.551.615)
        /// </summary>
        ULInt,

        /// <summary>
        /// Alias for USInt (8 Bit Unsigned Integer)
        /// </summary>
        Byte = USInt,

        /// <summary>
        /// Alias for UInt (16 Bit Unsigned Integer)
        /// </summary>
        Word = UInt,

        /// <summary>
        /// Alias for UDInt (32 Bit Unsigned Integer)
        /// </summary>
        DWord = UDInt,

        /// <summary>
        /// Alias for ULInt (64 Bit Unsigned Integer)
        /// </summary>
        LWord = ULInt,

        /// <summary>
        /// 32 Bit Signed Floating Point (single)
        /// </summary>
        Real,

        /// <summary>
        /// 64 Bit Signed Floating Point (double)
        /// </summary>
        LReal,

        /// <summary>
        /// 64 Bit BCD Encoded DateTime Value
        /// </summary>
        Datetime,

        /// <summary>
        /// 64 Bit BCD Encoded DateTime Value
        /// </summary>
        Date,

        /// <summary>
        /// String (Length must be supplied)
        /// </summary>
        String,

        /// <summary>
        /// Array of Bytes (Length must be supplied)
        /// </summary>
        ByteArray
    }

    internal class S7MemoryAddress
    {
        public int DataBlock { get; set; } = 0;
        public int Address { get; set; } = 0;
        public S7DataType DataType { get; set; } = S7DataType.Byte;
        public int Bit { get; set; } = 0;
        public S7AddressingMode Mode { get; set; } = S7AddressingMode.Unknown;
        public override string ToString()
        {
            switch (Mode)
            {
                case S7AddressingMode.DataBlock:
                    return $"{Mode}, DB={DataBlock}, offset={Address}, type={DataType}, bit={Bit}";
                case S7AddressingMode.FlagMemory:
                    return $"{Mode}, offset={Address}, type={DataType}";
                case S7AddressingMode.Output:
                    return $"{Mode}, offset={Address}, bit={Bit}";
                default:
                    return $"{Mode}, DB={DataBlock}, offset={Address}, type={DataType}, bit={Bit}";
            }
        }
    }

    /// <summary>
    /// S7 Address Parser; S7 has following addressing modes:
    ///  +-------------+--------------------+---------------+--------------+--------+------------------------+
    ///  | Memory Type |    Description     | Address Range |  Data Type   | Access |        Example         |
    ///  +-------------+--------------------+---------------+--------------+--------+------------------------+
    ///  | I, E        | Inputs             | *             | *            | R/W    | I3.2                   |
    ///  | Q, A        | Outputs            | *             | *            | R/W    | A9.1                   |
    ///  | PI, PE      | Peripheral Inputs  | *             | *            | R      |                        |
    ///  | PQ, PA      | Peripheral Outputs | *             | *            | R/W    |                        |
    ///  | M, F        | Flag Memory        | *             | *            | R/W    | MW180                  |
    ///  | DB          | Data Blocks        | *             | *            | R/W    | DB100.DBW8, DB16.DBB25 |
    ///  | T           | Timers             | T0..T65535    | Long (DWord) | R/W    |                        |
    ///  | C, Z        | Counters           | C0..C65535    | Short (Word) | R/W    |                        |
    ///  +-------------+--------------------+---------------+--------------+--------+------------------------+
    ///  * => Depends on S7 Data Type
    /// </summary>
    internal class S7AddressParser
    {
        private Dictionary<S7DataType, int> DataLengths = new Dictionary<S7DataType, int>()
        {
            [S7DataType.Bit] = 1,
            [S7DataType.SInt] = 1,
            [S7DataType.Int] = 2,
            [S7DataType.DInt] = 4,
            [S7DataType.LInt] = 8,
            [S7DataType.USInt] = 1,
            [S7DataType.UInt] = 2,
            [S7DataType.UDInt] = 4,
            [S7DataType.ULInt] = 8,
            [S7DataType.Byte] = 1,
            [S7DataType.Word] = 2,
            [S7DataType.DWord] = 4,
            [S7DataType.LWord] = 8,
            [S7DataType.Real] = 4,
            [S7DataType.LReal] = 8,
            [S7DataType.Datetime] = 8,
            [S7DataType.Date] = 8,
            [S7DataType.String] = -1,
            [S7DataType.ByteArray] = -1
        };

        public static bool IsValidAddress(string address)
        {
            try
            {
                var result = ParseAddress(address);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static S7MemoryAddress ParseAddress(string address)
        {
            Trace.TraceInformation($"Parsing Address {address}");

            if (Regex.IsMatch(address, @"^\d{1,4}$"))
            {
                return new S7MemoryAddress()
                {
                    Address = 0,
                    Bit = 0,
                    DataBlock = Convert.ToInt32(address),
                    DataType = S7DataType.Byte,
                    Mode = S7AddressingMode.DataBlock
                };
            }
            if (Regex.IsMatch(address, @"^DB\d{1,3}\.DB[BWX]\d{1,5}(\.[0-7])?$"))
            {
                return ParseDataBlockAddress(address);
            }
            if (Regex.IsMatch(address, @"^[QA]\d\.[0-7]$"))
            {
                return ParseIOAddress(address);
            }
            if (Regex.IsMatch(address, @"^M[BW]\d{1,3}$"))
            {
                return ParseMemoryAddress(address);
            }
            throw new ArgumentException(nameof(address), $"Unknown PLC Address Format ({address}).");
        }

        /// <summary>
        /// Parse address of type DB16.DBB5, DB16.DBW6, DB16.DBX80.0...
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static S7MemoryAddress ParseDataBlockAddress(string address)
        {
            var result = new S7MemoryAddress();
            result.Mode = S7AddressingMode.DataBlock;

            Regex pattern = new Regex(@"^(DB)(?<DataBlockAddress>\d{1,3})\.(DB)(?<DataType>[BWX])(?<Address>\d{1,5})(\.(?<Bit>[0-7]))?$");
            Match match = pattern.Match(address);

            if (match.Success)
            {
                if (match.Groups["Bit"].Success)
                {
                    result.Bit = Convert.ToInt32(match.Groups["Bit"].Value);
                }

                switch (match.Groups["DataType"].Value)
                {
                    case "B":
                        result.DataType = S7DataType.Byte;
                        break;
                    case "W":
                        result.DataType = S7DataType.Int;
                        break;
                    case "X":
                        result.DataType = S7DataType.Bit;
                        break;
                    default:
                        break;
                };

                result.DataBlock = Convert.ToInt32(match.Groups["DataBlockAddress"].Value);
                result.Address = Convert.ToInt32(match.Groups["Address"].Value);
            }
            return result;
        }

        /// <summary>
        /// Parse address of type MB, MW
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static S7MemoryAddress ParseMemoryAddress(string address)
        {
            var result = new S7MemoryAddress();
            result.Mode = S7AddressingMode.FlagMemory;

            Regex pattern = new Regex(@"^(M)(?<DataType>[BW])(?<Address>\d{1,3})$");
            Match match = pattern.Match(address);

            if (match.Success)
            {
                switch (match.Groups["DataType"].Value)
                {
                    case "B":
                        result.DataType = S7DataType.Byte;
                        break;
                    case "W":
                        result.DataType = S7DataType.Int;
                        break;
                    default:
                        break;
                };

                result.Address = Convert.ToInt32(match.Groups["Address"].Value);
            }

            return result;
        }

        /// <summary>
        /// Parse address of type A8.0, A8.1
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static S7MemoryAddress ParseIOAddress(string address)
        {
            var result = new S7MemoryAddress();
            result.Mode = S7AddressingMode.Output;

            Regex pattern = new Regex(@"^([QA])(?<Slot>\d)\.(?<Bit>[0-7])$");
            Match match = pattern.Match(address);

            if (match.Success)
            {
                result.Address = Convert.ToInt32(match.Groups["Slot"].Value);
                result.Bit = Convert.ToInt32(match.Groups["Bit"].Value);
            }

            return result;
        }


    }

}