using System;
using System.Net;

public class IPAddressParser
{
    const int DefaultPort = 5000; 
    public static bool Parse(string ipAddressWithPortText, out IPAddress ipAddress, out int port)
    {
        var idx = ipAddressWithPortText.IndexOf(':');
        port = DefaultPort;    
        if (idx != -1)
        {
            port = int.Parse(ipAddressWithPortText.Substring(idx+1));
        }
        ipAddress = ParseIpAddress(ipAddressWithPortText.Substring(0, idx));
        return ipAddress != null;
    }
    
    private static IPAddress ParseIpAddress(string ipAddress)
    {
        try
        {
            // Create an instance of IPAddress for the specified address string (in
            // dotted-quad, or colon-hexadecimal notation).
            IPAddress address = IPAddress.Parse(ipAddress);

            // Display the address in standard notation.
            Console.WriteLine("Parsing your input string: " + "\"" + ipAddress + "\"" + " produces this address (shown in its standard notation): "+ address.ToString());
            return address;
        }

        catch(ArgumentNullException e)
        {
            Console.WriteLine("ArgumentNullException caught!!!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }

        catch(FormatException e)
        {
            Console.WriteLine("FormatException caught!!!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }

        catch(Exception e)
        {
            Console.WriteLine("Exception caught!!!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        return null;
    }
}    
