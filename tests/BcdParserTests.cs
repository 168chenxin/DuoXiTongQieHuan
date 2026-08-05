using System;
using System.Collections.Generic;
using DualBootSwitcher;

internal static class BcdParserTests
{
    private const string LoaderOutput = @"
Windows Boot Loader
-------------------
identifier              {62ef9ac8-8268-11f1-a95e-fc9d056c3957}
device                  partition=F:
path                    \Windows\system32\winload.efi
description             Windows 10 F drive

Windows Boot Loader
-------------------
identifier              {62fe5b36-8268-11f1-a95e-fc9d056c3957}
device                  partition=C:
path                    \Windows\system32\winload.efi
description             Windows 10 C drive
";

    private const string BootManagerOutput = @"
Windows Boot Manager
--------------------
identifier              {bootmgr}
default                 {62fe5b36-8268-11f1-a95e-fc9d056c3957}
displayorder            {62fe5b36-8268-11f1-a95e-fc9d056c3957}
                        {62ef9ac8-8268-11f1-a95e-fc9d056c3957}
timeout                 15
";

    private const string LoaderOutputWithHiddenEntry = LoaderOutput + @"
Windows Boot Loader
-------------------
identifier              {99999999-9999-9999-9999-999999999999}
device                  ramdisk=[C:]\Recovery\WindowsRE\Winre.wim
path                    \Windows\system32\winload.efi
description             Hidden recovery loader
";

    private const string ChineseLoaderOutput = @"
Windows 启动加载器
-------------------
标识符                  {11111111-1111-1111-1111-111111111111}
设备                    partition=D:
描述                    Windows 11
";

    private const string ChineseBootManagerOutput = @"
Windows 启动管理器
--------------------
标识符                  {bootmgr}
默认                    {11111111-1111-1111-1111-111111111111}
显示顺序                {11111111-1111-1111-1111-111111111111}
";

    private static int Main()
    {
        try
        {
            ParsesEnglishLoaders();
            ParsesChineseProperties();
            FindsDefaultIdentifier();
            ParsesDisplayOrder();
            FiltersOutLoadersNotInTheBootMenu();
            DoesNotSelectEntriesWithoutBootMenuOrder();
            ComparesIdentifiersWithoutCaseSensitivity();
            ReadsTheActiveBcdStore();
            Console.WriteLine("BcdParser tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void ParsesEnglishLoaders()
    {
        List<BootEntry> entries = BcdParser.ParseBootLoaders(LoaderOutput);

        AssertEqual(2, entries.Count, "Expected two Windows boot entries.");
        AssertEqual("Windows 10 F drive", entries[0].Description, "Expected first description.");
        AssertEqual("F:", entries[0].Device, "Expected F: partition.");
        AssertEqual("C:", entries[1].Device, "Expected C: partition.");
    }

    private static void ParsesChineseProperties()
    {
        List<BootEntry> entries = BcdParser.ParseBootLoaders(ChineseLoaderOutput);

        AssertEqual(1, entries.Count, "Expected one Chinese Windows boot entry.");
        AssertEqual("Windows 11", entries[0].Description, "Expected Chinese fixture description.");
        AssertEqual("D:", entries[0].Device, "Expected D: partition.");
    }

    private static void FindsDefaultIdentifier()
    {
        AssertEqual(
            "{62fe5b36-8268-11f1-a95e-fc9d056c3957}",
            BcdParser.ParseDefaultIdentifier(BootManagerOutput),
            "Expected English default identifier.");
        AssertEqual(
            "{11111111-1111-1111-1111-111111111111}",
            BcdParser.ParseDefaultIdentifier(ChineseBootManagerOutput),
            "Expected Chinese default identifier.");
    }

    private static void ComparesIdentifiersWithoutCaseSensitivity()
    {
        AssertTrue(
            BcdParser.IdentifiersMatch("{ABCDEF}", "{abcdef}"),
            "Identifiers should compare without case sensitivity.");
        AssertTrue(
            !BcdParser.IdentifiersMatch("{ABCDEF}", "{123456}"),
            "Different identifiers should not match.");
    }

    private static void ParsesDisplayOrder()
    {
        List<string> englishOrder = BcdParser.ParseDisplayOrder(BootManagerOutput);
        List<string> chineseOrder = BcdParser.ParseDisplayOrder(ChineseBootManagerOutput);

        AssertEqual(2, englishOrder.Count, "Expected two entries in the English display order.");
        AssertEqual(
            "{62fe5b36-8268-11f1-a95e-fc9d056c3957}",
            englishOrder[0],
            "Expected the first English display-order entry.");
        AssertEqual(1, chineseOrder.Count, "Expected one entry in the Chinese display order.");
    }

    private static void FiltersOutLoadersNotInTheBootMenu()
    {
        List<BootEntry> discoveredEntries = BcdParser.ParseBootLoaders(LoaderOutputWithHiddenEntry);
        List<string> displayOrder = BcdParser.ParseDisplayOrder(BootManagerOutput);
        List<BootEntry> displayedEntries = BcdService.SelectDisplayedEntries(discoveredEntries, displayOrder);

        AssertEqual(3, discoveredEntries.Count, "Expected the hidden loader in the unfiltered entries.");
        AssertEqual(2, displayedEntries.Count, "Expected only boot-menu entries after filtering.");
        AssertEqual("C:", displayedEntries[0].Device, "Expected display-order sorting to put C: first.");
        AssertEqual("F:", displayedEntries[1].Device, "Expected display-order sorting to put F: second.");
    }

    private static void DoesNotSelectEntriesWithoutBootMenuOrder()
    {
        List<BootEntry> discoveredEntries = BcdParser.ParseBootLoaders(LoaderOutput);
        List<BootEntry> displayedEntries = BcdService.SelectDisplayedEntries(
            discoveredEntries,
            new List<string>());

        AssertEqual(0, displayedEntries.Count, "Expected no selectable entries without a boot-menu order.");
    }

    private static void ReadsTheActiveBcdStore()
    {
        List<BootEntry> entries = BcdService.LoadEntries();
        bool hasDefaultEntry = false;

        foreach (BootEntry entry in entries)
        {
            hasDefaultEntry = hasDefaultEntry || entry.IsDefault;
        }

        AssertTrue(entries.Count > 0, "Expected at least one Windows boot entry from the active BCD store.");
        AssertTrue(hasDefaultEntry, "Expected a default Windows boot entry from the active BCD store.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
