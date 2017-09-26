# read-more-api [![CircleCI](https://circleci.com/gh/defining-technology/read-more-api.svg?style=svg)](https://circleci.com/gh/defining-technology/read-more-api)
Backend for [Readmore extension](https://github.com/defining-technology/readmore-chrome).

## Architecture Decisions

This project uses [Architecture Decision Records (ADRs)](http://thinkrelevance.com/blog/2011/11/15/documenting-architecture-decisions) to record decisions made throughout the life of this project. The command line tool [adr-tools](https://github.com/npryce/adr-tools) used for managing these ADRs.

The ADRs for this project can be found [here](ReadMoreAPI/doc/adr).

## Building

The project can be built and run with Visual Studio 2017, or by using the dotnet command line tool.

The solution uses Docker, meaning that the only Windows version that can be used for building this project is Windows 10 Pro.

## Contributing

PRs and issues are welcome, be it to do with code or documentation. 

An ADR should be included in a PR when a significant decision around the implementation of this project is made. If the decision outlined by an ADR is to be changed, the PR should include the superseeding of the relevant existing ADR with a new ADR (adr-tools simplifies this process).
