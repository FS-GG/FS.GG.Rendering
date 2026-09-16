module Scene.SvgBrowser.Policy.Tests.PersistencePolicyTests

open Expecto
open FS.GG.UI.Scene.SvgBrowser

let hash value = "h:" + value

let member' path content =
    {
        Path = path
        Content = content
        Hash = hash content
    }

[<Tests>]
let tests =
    testList
        "browser archive"
        [
            test "complete archives normalize to stable path order" {
                let archive =
                    {
                        Members = [ member' "data/save/b.json" "b"; member' "data/save/a.json" "a" ]
                    }

                let result =
                    BrowserArchive.validate hash [ "data/save/a.json"; "data/save/b.json" ] archive

                Expect.equal
                    result
                    (Ok
                        {
                            Members = [ member' "data/save/a.json" "a"; member' "data/save/b.json" "b" ]
                        })
                    "stable order"
            }
            test "traversal, duplicate, missing, unexpected and hash defects accumulate before commit" {
                let archive =
                    {
                        Members =
                            [
                                member' "../save.json" "x"
                                member' "data/save/a.json" "a"
                                { member' "data/save/a.json" "bad" with
                                    Hash = "wrong"
                                }
                                member' "data/save/extra.json" "e"
                            ]
                    }

                let issues =
                    match BrowserArchive.validate hash [ "data/save/a.json"; "data/save/missing.json" ] archive with
                    | Error value -> value
                    | Ok _ -> failtest "accepted"

                Expect.contains issues (BrowserArchiveIssue.InvalidPath "../save.json") "traversal"
                Expect.contains issues (BrowserArchiveIssue.DuplicatePath "data/save/a.json") "duplicate"
                Expect.contains issues (BrowserArchiveIssue.MissingMember "data/save/missing.json") "missing"
                Expect.contains issues (BrowserArchiveIssue.UnexpectedMember "data/save/extra.json") "unexpected"
                Expect.contains issues (BrowserArchiveIssue.HashMismatch("data/save/a.json", "wrong", "h:bad")) "hash"
            }
            test "empty archives refuse" {
                Expect.equal
                    (BrowserArchive.validate hash [] { Members = [] })
                    (Error[BrowserArchiveIssue.EmptyArchive])
                    "empty has no commit subject"
            }
        ]
