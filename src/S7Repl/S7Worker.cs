using Sharp7;
using System;

namespace S7ReplApp
{
    internal class S7Worker
    {
        private S7Client _plcClient;

        public void Connect(string ip, int rack, int slot)
        {
            Disconnect();

            var result = _plcClient.ConnectTo(ip, rack, slot);

            if (result != 0)
                throw new System.Exception(_plcClient.ErrorText(result));
        }

        public void Disconnect()
        {
            var result = _plcClient?.Disconnect() ?? 0;

            if (result != 0)
                throw new System.Exception(_plcClient.ErrorText(result));
        }

        public S7Worker()
        {
            _plcClient = new S7Client();
        }

        public void Dump(int address, int offset, int length, bool useDecimalValues = false)
        {
            int printLength = 16;

            var buffer = new byte[length];

            var result = _plcClient.DBRead(address, offset, length, buffer);

            if (result != 0)
                throw new System.Exception(_plcClient.ErrorText(result));

            int counter = 0;

            if (useDecimalValues)
                Console.Write($"{address,5} - ");
            else
                Console.Write($"0x{address:X4} - ");

            foreach (var value in buffer)
            {

                if (useDecimalValues)
                    Console.Write($"{value,3} ");
                else
                    Console.Write($"0x{value:X2} ");

                if (++counter % printLength == 0)
                {
                    Console.WriteLine();
                    address += printLength;

                    if (useDecimalValues)
                        Console.Write($"{address,5} - ");
                    else
                        Console.Write($"0x{address:X4} - ");
                }
            }

            Console.WriteLine();
            Console.WriteLine();
        }

    }
}
