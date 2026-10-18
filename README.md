# Where Darkness Dwells

A single-player dark-fantasy roguelite deckbuilder for Unity. A party of three heroes investigates the Black
Keep, gathers clues, and confronts the thing at its heart.

- Design: [GAME_DESIGN.md](GAME_DESIGN.md)
- Roadmap and current milestone: [MILESTONES.md](MILESTONES.md)
- Architecture and conventions: [CLAUDE.md](CLAUDE.md)

## Play on the web

The game is published on GitHub Pages from GitHub releases, at https://davegp.github.io/where-darkness-dwells/.

One-time setup: on GitHub, open the repository's **Settings > Pages** and set **Source** to **GitHub Actions**.

For each release:

1. In Unity, run **Where Darkness Dwells > Build for Web**. It needs the Web Build Support module. It applies the web
   settings, builds the Run scene to `Unity/Builds/Web`, and zips it to `Unity/Builds/where-darkness-dwells-web.zip`.
2. On GitHub, open **Releases > Draft a new release**, create a tag such as `v0.1.0`, attach the zip and publish.
3. The **Publish web game** workflow puts the build live in a minute or two. To republish an older release, run the
   workflow by hand from the Actions tab and give it that release's tag.

The build includes the Synty art from the machine it was built on, compiled into the game. The art's source files
never go in the repository.
