namespace Tests

open NUnit.Framework
open Binnaculum.Core.Database.DatabaseModel
open OptionTradeExtensions

[<TestFixture>]
type OptionTradeLinkingTests() =

    [<Test>]
    member _.``Opening code detection identifies open trades``() =
        Assert.Multiple(fun () ->
            Assert.That(isOpeningCode OptionCode.BuyToOpen, Is.True)
            Assert.That(isOpeningCode OptionCode.SellToOpen, Is.True)
            Assert.That(isOpeningCode OptionCode.BuyToClose, Is.False))

    [<Test>]
    member _.``Closing code detection identifies close trades``() =
        Assert.Multiple(fun () ->
            Assert.That(isClosingCode OptionCode.BuyToClose, Is.True)
            Assert.That(isClosingCode OptionCode.SellToClose, Is.True)
            Assert.That(isClosingCode OptionCode.BuyToOpen, Is.False))

    [<Test>]
    member _.``BuyToClose links to SellToOpen``() =
        let expected = [| OptionCode.SellToOpen |]
        let actual = getOpeningCodesForClosing OptionCode.BuyToClose |> List.toArray
        Assert.That(actual, Is.EqualTo<OptionCode[]>(expected))

    [<Test>]
    member _.``Expired attempts both opening directions``() =
        let expected = [| OptionCode.SellToOpen; OptionCode.BuyToOpen |]
        let actual = getOpeningCodesForClosing OptionCode.Expired |> List.toArray
        Assert.That(actual, Is.EqualTo<OptionCode[]>(expected))
