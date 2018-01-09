﻿namespace GWallet.Frontend.XF

open System
open System.Threading.Tasks

open Xamarin.Forms
open Xamarin.Forms.Xaml

open GWallet.Backend

type WelcomePage() =
    inherit ContentPage()

    let _ = base.LoadFromXaml(typeof<WelcomePage>)

    let mainLayout = base.FindByName<StackLayout>("mainLayout")
    let passphrase = mainLayout.FindByName<Entry>("passphraseEntry")
    let passphraseConfirmation = mainLayout.FindByName<Entry>("passphraseEntryConfirmation")

    member this.OnCreateButtonClicked(sender: Object, args: EventArgs) =
        if (passphrase.Text <> passphraseConfirmation.Text) then
            this.DisplayAlert("Alert", "Passphrases don't match, please try again", "OK") |> ignore
        else
            let createButton = mainLayout.FindByName<Button>("createButton")
            createButton.IsEnabled <- false
            createButton.Text <- "Creating..."

            Task.Run(fun _ ->
                for currency in Currency.GetAll() do
                    Account.CreateNormalAccount currency passphrase.Text |> ignore
            ).ContinueWith(fun _ ->
                Device.BeginInvokeOnMainThread(fun _ ->
                    this.Navigation.PushModalAsync(BalancesPage()) |> ignore
                )
            ) |> FrontendHelpers.DoubleCheckCompletion

    member this.OnPassphraseTextChanged(sender: Object, args: EventArgs) =

        let createButton = mainLayout.FindByName<Button>("createButton")
        if (passphrase.Text <> null && passphrase.Text.Length > 0 &&
            passphraseConfirmation.Text <> null && passphraseConfirmation.Text.Length > 0) then
            createButton.IsEnabled <- true

