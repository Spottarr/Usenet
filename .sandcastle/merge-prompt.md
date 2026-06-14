# TASK

Open a pull request for each completed branch below, targeting `{{BASE_BRANCH}}`.

This repo is **rebase-merge only** — merge commits and squash merges are disabled, and
all changes land via PR. Do **not** merge branches into `{{BASE_BRANCH}}` locally and do
**not** close the issues directly; the PR merge closes them via a `Closes #NN` footer.

Branches:

{{BRANCHES}}

# PROCESS

For each branch:

1. Push it to the remote: `git push -u origin <branch>`.
2. Open a PR against `{{BASE_BRANCH}}`:
   `gh pr create --base {{BASE_BRANCH}} --head <branch> --title "<title>" --body "<body>"`
   - Use a concise, imperative, sentence-case title (e.g. `Add NZB segment validation`).
     This repo does not use Conventional Commits.
   - In the body, summarize the change and add a `Closes #NN` footer for the issue the
     branch implements so the PR merge closes it automatically.
3. If the branch cannot be pushed or the PR cannot be opened (e.g. it is already behind
   `{{BASE_BRANCH}}` and conflicts), note it and continue with the next branch rather
   than forcing a local merge.

Do not enable auto-merge or merge the PRs yourself — they go through CI and review.

# ISSUE MAPPING

Each branch implements one of these issues; use it for the PR title and the
`Closes #NN` footer:

{{ISSUES}}

Once you've opened every PR you can, output <promise>COMPLETE</promise>.
