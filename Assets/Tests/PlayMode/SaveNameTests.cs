/*---------------------------
| File: SaveNameTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    // Which playthrough names New Game accepts: any name that works as a file name on every platform.
    public class SaveNameTests
    {
        /*----------------------------------------------------------------------------------------------------------------
        | --- IsValidSaveName_RefusesNamesThatCantBeAFileName: Names a file can't have, on any platform, are refused --- |
        ----------------------------------------------------------------------------------------------------------------*/
        [TestCase("a/b")]
        [TestCase("a\\b")]
        [TestCase("con:")]
        [TestCase("CON")]
        [TestCase("aux.txt")]
        [TestCase("what?")]
        [TestCase("name.")]
        [TestCase(" padded ")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("hero_autosave")]
        [Description("Names a file can't have on some platform (separators, reserved device names, forbidden characters, a trailing dot or space), empty names, and names that would share files with another playthrough's autosave are refused.")]
        public void IsValidSaveName_RefusesNamesThatCantBeAFileName(string saveName)
        {
            Assert.That(SavingSystem.IsValidSaveName(saveName), Is.False);
        }

        /*--------------------------------------------------------------------------------------------------
        | --- IsValidSaveName_AcceptsOrdinaryNames: Ordinary names, spaces and dots included, are fine --- |
        --------------------------------------------------------------------------------------------------*/
        [TestCase("Hero")]
        [TestCase("Chandler's Run 2")]
        [TestCase("v1.2 playthrough")]
        [TestCase("Console")]
        [Description("Ordinary names, with spaces, apostrophes, digits and inner dots, are accepted; a reserved device name only counts on its own.")]
        public void IsValidSaveName_AcceptsOrdinaryNames(string saveName)
        {
            Assert.That(SavingSystem.IsValidSaveName(saveName), Is.True);
        }
    }
}