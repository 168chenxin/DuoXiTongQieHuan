using System;
using System.Collections.Generic;
using DualBootSwitcher;

internal static class FirmwareBootParserTests
{
    private static int Main()
    {
        try
        {
            ParsesAndDetectsNetworkEntry();
            IgnoresFirmwareManagerEntry();
            SelectsTheMostSpecificNetworkEntry();
            DoesNotTreatOnboardAsNetworkBoot();
            DescribesNetworkProtocol();
            Console.WriteLine("Firmware boot parser tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void ParsesAndDetectsNetworkEntry()
    {
        const string output = "Firmware Boot Manager\r\n---------------------\r\nidentifier              {fwbootmgr}\r\ndisplayorder            {11111111-1111-1111-1111-111111111111}\r\n\r\nWindows Boot Manager\r\n---------------------\r\nidentifier              {11111111-1111-1111-1111-111111111111}\r\ndescription             Windows Boot Manager\r\npath                    \\EFI\\Microsoft\\Boot\\bootmgfw.efi\r\n\r\nNetwork PXE\r\n---------------------\r\nidentifier              {22222222-2222-2222-2222-222222222222}\r\ndescription             IPv4 Network\r\ndevice                  VenHw(1234)\r\npath                    \\EFI\\PXE\\bootx64.efi";
        List<FirmwareBootEntry> entries = FirmwareBootParser.ParseEntries(output);
        AssertEqual(2, entries.Count, "The firmware manager entry should be excluded.");
        AssertTrue(entries[1].IsNetworkBoot, "IPv4/PXE entries should be detected as network boot.");
    }

    private static void IgnoresFirmwareManagerEntry()
    {
        List<FirmwareBootEntry> entries = FirmwareBootParser.ParseEntries(
            "identifier {fwbootmgr}\r\ndescription Firmware Boot Manager");
        AssertEqual(0, entries.Count, "The firmware manager pseudo-entry is not selectable.");
    }

    private static void SelectsTheMostSpecificNetworkEntry()
    {
        var entries = new List<FirmwareBootEntry>
        {
            new FirmwareBootEntry("{local}", "Windows Boot Manager", "partition=C:", "\\EFI\\Microsoft\\Boot\\bootmgfw.efi"),
            new FirmwareBootEntry("{ipv6}", "UEFI Network IPv6", "VenHw(5678)", string.Empty),
            new FirmwareBootEntry("{ipv4}", "UEFI PXE IPv4 Realtek PCIe", "VenHw(1234)", string.Empty)
        };

        FirmwareBootEntry selected = FirmwareBootParser.FindBestNetworkBootEntry(entries);
        AssertEqual("{ipv4}", selected.Identifier, "PXE IPv4 should be preferred when several network entries exist.");
    }

    private static void DoesNotTreatOnboardAsNetworkBoot()
    {
        var entry = new FirmwareBootEntry("{setup}", "Onboard Device Configuration", string.Empty, string.Empty);
        AssertTrue(!entry.IsNetworkBoot, "The generic word Onboard must not identify a network boot entry.");
    }

    private static void DescribesNetworkProtocol()
    {
        var entry = new FirmwareBootEntry("{pxe}", "PXE IPv4 Network Boot", "VenHw(1234)", string.Empty);
        AssertEqual("PXE / IPv4", entry.NetworkType, "The protocol detail should be readable in the detection result.");
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }
}
