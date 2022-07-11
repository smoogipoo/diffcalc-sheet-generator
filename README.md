# SR/PP Spreadsheet Generator

Generates spreadsheets for comparing osu! SR and PP changesets.

## Requirements

- `docker >= 20.10.16`
- `docker-compose >= 2.5.1`

## Usage

- Download and extract relavant database exports from https://data.ppy.sh into `sql/`.
- Download and extract beatmap exports from https://data.ppy.sh into `beatmaps/`.
    - Subdirectories are *not* supported (ensure you have `beatmaps/*.osu`, etc...).
- Copy `.env.sample` to `.env` and update as required.
- Run `docker-compose up`.

## GitHub Token

This project makes use of [GitHub CLI](https://github.com/cli/cli), which requires a GitHub token.

To create a token, head to https://github.com/settings/tokens, and create a token with read-only permissions (all boxes unchecked).

## Google Service Account

Spreadsheets are uploaded to a Google Service Account, and exposed with read-only permissions to the wider audience.

1. Create a project at https://console.cloud.google.com
2. Enable the `Google Sheets` and `Google Drive` APIs.
3. Create a Service Account
4. Generate a key in the JSON format. **DO NOT POST THIS KEY.**
5. Store the key in a secure location and set `GOOGLE_CREDENTIALS_FILE` in `.env` to the key file's location.