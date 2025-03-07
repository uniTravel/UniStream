[<AutoOpen>]
module Account.TestFixture.Account

open UniStream.Domain
open Account.Domain


let createAccount agent aggId comId com =
    Aggregator.create<Account, CreateAccount, AccountCreated> agent aggId comId com

let verifyAccount agent aggId comId com =
    Aggregator.apply<Account, VerifyAccount, AccountVerified> agent aggId comId com

let approveAccount agent aggId comId com =
    Aggregator.apply<Account, ApproveAccount, AccountApproved> agent aggId comId com

let limitAccount agent aggId comId com =
    Aggregator.apply<Account, LimitAccount, AccountLimited> agent aggId comId com

let getAccount agent aggId = Aggregator.get<Account> agent aggId
