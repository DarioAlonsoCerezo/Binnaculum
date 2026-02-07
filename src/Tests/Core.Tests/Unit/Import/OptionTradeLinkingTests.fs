namespace Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Database.DatabaseModel
open OptionTradeExtensions

[<TestClass>]
type OptionTradeLinkingTests() =

    [<TestMethod>]
    member _.``Opening code detection identifies open trades``() =
        Assert.Multiple(fun () ->
            Assert.IsTrue(isOpeningCode OptionCode.BuyToOpen)
            Assert.IsTrue(isOpeningCode OptionCode.SellToOpen)
            Assert.IsFalse(isOpeningCode OptionCode.BuyToClose))

    [<TestMethod>]
    member _.``Closing code detection identifies close trades``() =
        Assert.Multiple(fun () ->
            Assert.IsTrue(isClosingCode OptionCode.BuyToClose)
            Assert.IsTrue(isClosingCode OptionCode.SellToClose)
            Assert.IsFalse(isClosingCode OptionCode.BuyToOpen))

    [<TestMethod>]
    member _.``BuyToClose links to SellToOpen``() =
        let expected = [| OptionCode.SellToOpen |]
        let actual = getOpeningCodesForClosing OptionCode.BuyToClose |> List.toArray
        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Expired attempts both opening directions``() =
        let expected = [| OptionCode.SellToOpen; OptionCode.BuyToOpen |]
        let actual = getOpeningCodesForClosing OptionCode.Expired |> List.toArray
        CollectionAssert.AreEqual(expected, actual)
