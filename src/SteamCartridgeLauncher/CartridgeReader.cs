using NdefLibrary.Ndef;

using PCSC;
using PCSC.Monitoring;

namespace SteamCartridgeLauncher;

public class CartridgeReader
{
    private readonly ISCardContext _context;
    private readonly string _readerName;

    public bool IsReadingCard
    {
        get;
        private set;
    }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    private CartridgeReader(ISCardContext context)
    {
        _context = context;
        _readerName = context.GetReaders()[0];
        IsReadingCard = false;
    }

    public static CartridgeReader InitializeCartridgeReader()
    {
        // 1 - Initialize the context
        ISCardContext context = ContextFactory.Instance.Establish(SCardScope.System);

        // 2 - Create cartridge reader
        CartridgeReader reader = new(context);

        // 3 - Initialize the event monitor 
        ISCardMonitor monitor = MonitorFactory.Instance.Create(SCardScope.System);

        // 4 - Add trigger on card inserted
        monitor.CardInserted += (sender, args) =>
        {
            reader.CardInsertedEvent(sender, args);
        };

        monitor.CardRemoved += (sender, args) =>
        {
            reader.CardRemovedEvent(sender, args);
        };

        // 5 - Start monitor only for one card reader
        monitor.Start(reader._readerName);

        // 6 - Return new CartridgeReader
        return reader;
    }

    ///////////////////////
    //// EVENT MANAGER ////
    ///////////////////////
    private void CardInsertedEvent(object sender, CardStatusEventArgs args)
    {
        Console.WriteLine("Card inserted in: " + _readerName);
        IsReadingCard = true;
        CardInserted?.Invoke();
    }

    private void CardRemovedEvent(object sender, CardStatusEventArgs args)
    {
        Console.WriteLine("Card removed in: " + _readerName);
        IsReadingCard = false;
        CardRemoved?.Invoke();
    }

    public event Action? CardInserted;
    public event Action? CardRemoved;

    /////////////////////////
    //// READ MANAGEMENT ////
    /////////////////////////
    public string? ReadCard()
    {
        // 0 - If no card, nothing to do
        if (!IsReadingCard)
            return null;

        // 1 - Instantiate card reader
        using SCardReader reader = new(_context);
        reader.Connect(_readerName, SCardShareMode.Shared, SCardProtocol.Any);

        // 2 - Read first block
        byte block = 4;
        byte[] data = ReadBlock(reader, block);

        // 3 - If first block does not contain NDEF opening byte, ignore
        if (data[0] != 0x03)
        {
            Console.WriteLine("No message found");
            return null;
        }

        // 4 - Append message to raw list 
        List<byte> rawList = [.. data.Take(4)];

        // 5 - Read message on card
        int len = data[1];
        int expectedSize = data[1] + 3;
        for (int i = 4; i < expectedSize; i += 4)
        {
            // 5.1 - Increase block number
            block++;

            // 5.2 - Read block
            data = ReadBlock(reader, block);

            // 5.3 - Append message
            rawList.AddRange(data.Take(4));
        }

        // 6 - Retrieve ndef byte message
        byte[] message = new byte[len];
        Array.Copy(rawList.ToArray(), 2, message, 0, len);
        Console.WriteLine("message read : " + BitConverter.ToString(message));

        // 7 - Decode ndef message
        try
        {
            NdefRecord ndef = NdefMessage.FromByteArray(message).First();
            if (ndef.CheckSpecializedType(false) == typeof(NdefTextRecord))
            {
                string steamURI = new NdefTextRecord(ndef).Text;
                return steamURI;
            }
        }
        catch (Exception) { }

        // 8 - Otherwise return null
        return null;
    }

    private static byte[] ReadBlock(SCardReader reader, byte block)
    {
        // 1 - Define read command
        byte[] readCommand =
        [
            0xFF, // Class
            0xB0, // Read
            0x00, // Parameter 1 (empty)
            block, // Parameter 2 (block number)
            0x04, // Length of block
        ];

        // 2 - Execute command
        byte[] receiveBuffer = new byte[256];
        reader.Transmit(readCommand, ref receiveBuffer);
        //Console.WriteLine("Reading " + BitConverter.ToString(receiveBuffer) + " in block " + block);

        // 3 - Return byte array
        return receiveBuffer;
    }

    //////////////////////////
    //// WRITE MANAGEMENT ////
    //////////////////////////
    public void WriteCard(string input)
    {
        // 0 - If no card, nothing to do
        if (!IsReadingCard)
            return;

        // 1 - Instantiate card reader
        using SCardReader reader = new(_context);
        reader.Connect(_readerName, SCardShareMode.Shared, SCardProtocol.Any);

        // 2 - Transpose message using Ndef tool
        NdefMessage message = [];
        NdefTextRecord textRecord = new()
        {
            Text = input
        };
        message.Add(textRecord);
        byte[] ndefBytes = message.ToByteArray();
        Console.WriteLine("Writing " + BitConverter.ToString(ndefBytes));

        // 3 - Append TLV format encapsulators
        byte[] tlv = new byte[ndefBytes.Length + 3];

        tlv[0] = 0x03; // NDEF TLV tag
        tlv[1] = (byte)ndefBytes.Length; // length
        Array.Copy(ndefBytes, 0, tlv, 2, ndefBytes.Length);
        tlv[^1] = 0xFE; // terminator

        // 4 - Write message on card
        byte block = 4;
        for (int i = 0; i < tlv.Length; i += 4)
        {
            // 4.1 - Divide the data into chunk of 4 bytes
            byte[] chunk = new byte[4];
            byte len = (byte)Math.Min(4, tlv.Length - i);
            Array.Copy(tlv, i, chunk, 0, len);

            // 4.2 - Define the write command
            WriteBlock(reader, block, chunk);

            // 4.3 - Iterate to next block
            block++;
        }
    }

    private static void WriteBlock(SCardReader reader, byte block, byte[] data)
    {
        // 1 - Define the write command
        byte len = (byte)data.Length;

        byte[] writeCommand = new byte[5 + len];
        writeCommand[0] = 0xFF; // Class
        writeCommand[1] = 0xD6; // Write
        writeCommand[2] = 0x00; // Parameter 1
        writeCommand[3] = block; // Block number
        writeCommand[4] = 0x04; // Size of the block

        Array.Copy(data, 0, writeCommand, 5, len);

        // 2 - Write the block
        //Console.WriteLine("Writing " + BitConverter.ToString(data) + " in block " + block);
        byte[] receiveBuffer = new byte[256];
        reader.Transmit(writeCommand, ref receiveBuffer);

        // 3 - Check success
        if (receiveBuffer[0] != 0x90)
        {
            Console.WriteLine("Write failed: " + BitConverter.ToString(receiveBuffer));
        }
    }
}