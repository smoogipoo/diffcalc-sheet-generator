###
### Base image
###

FROM ubuntu:24.04 AS base

# Important requirements (used by every image):
# - dotnet-8
# - mysql-client
# - gh-cli
# Everything else is various supporting tooling.
RUN apt update \
    && apt install -y \
        wget \
        dotnet-sdk-8.0 \
        pv \
        git \
        mysql-client \
    && mkdir -p -m 755 /etc/apt/keyrings \
    && wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
    && chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt update \
    && apt install -y \
        gh

###
### Generator image
###

FROM base AS generator

WORKDIR /workdir
COPY . ./

WORKDIR /workdir/generator
RUN dotnet publish -c Release -o /out
