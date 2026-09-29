# Contributing to MediaIsland

Thanks for your interest! Bug reports, ideas and pull requests are all welcome.

## Branches

| Branch | Purpose |
|---|---|
| `dev` | Default branch. **All pull requests target `dev`.** |
| `main` | Stable code. Locked; updated only by the maintainer through a `dev` → `main` pull request. |

Pull requests opened against `main` fail the branch check; retarget them to `dev`.

## Workflow

1. Fork the repository and clone your fork.
2. Create a branch from `dev`:
   ```bash
   git checkout -b fix/short-description dev
   ```
3. Make your change, build it and try it with a real player (Spotify, YouTube in a browser, …).
4. Push to your fork and open a pull request into `dev`.
5. The CI build must pass before the pull request can be merged.

For larger changes, open an issue first so we can agree on the approach before you invest time.

## Building

Requirements: Windows 10 2004+ or Windows 11, .NET 10 SDK.

```bash
dotnet build
dotnet run
```

## Guidelines

- Keep pull requests focused: one fix or feature per pull request.
- Match the style of the surrounding code.
- For UI changes, add a before/after screenshot to the pull request.

## License

By contributing, you agree that your contributions are licensed under the [Apache License 2.0](LICENSE).
