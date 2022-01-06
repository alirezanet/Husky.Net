# Introduction

Husky improves your commits and more 🐶 woof!

Run linters against staged git files and don't let 💩 slip into your code base!

These two quotes from [husky](https://github.com/typicode/husky) and [lint-staged](https://github.com/okonet/lint-staged) JS tools inspired me to create Husky.Net for dotnet developers. it provides a simple and native way to do both also has a lot of other cool features. You can use it to lint your commit messages, run tests, lint code, etc. when you commit or push. 🚀🚀

## Features

- User-friendly messages
- Supports macOS, Linux and Windows
- Git GUIs
- Custom directories
- Monorepo
- 🔥 Internal task runner!
- 🔥 Multiple file states (staged, last-commit, git-files, etc...)
- 🔥 Compatible with dotnet-format
- 🔥 User-define arg variables
- 🔥 CSharp scripts (csx)
- 🔥 Supports gitflow hooks

-_A lot of features are coming soon, stay tuned! 👁️‍🗨️👀_

## Why use Hooks and Husky.Net?

We, as developers, love platforms like GitHub, GitLab, Atlassian, Azure DevOps etc., as our managed git system and collaboration platform. We also love clean code and keep inventing new linters and rules to enforce it. In my opinion, every commit should allow the codebase to deploy to production. There is nothing worse than commits like “fixed style errors” or “fixed build”. These are often small mistakes you want to know as early as possible in your development cycle. You don’t want to break the build for the next developer because he pulled your ‘mistake’ or waste precious build minutes of your CI server. Say you have asked your teammate to review your code; in the meantime, the build server rejects your code. That means you have to go back and fix this, and your teammate has to come back and possibly review again after the changes (i.e., approvals reset on new commit). Doing so would waste a lot of time and effort.

Husky.Net offers a very simple way to start using git hooks or running certain tasks, write and run custom scripts and more ...
