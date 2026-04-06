.DEFAULT_GOAL := help

.PHONY: help build test pack clean format

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-10s\033[0m %s\n", $$1, $$2}'

build: ## Build the solution
	dotnet build Cqrs.slnx

test: ## Run all tests
	dotnet test Cqrs.slnx --logger "console;verbosity=normal"

pack: ## Pack NuGet packages to ./artifacts/nupkgs
	dotnet pack Cqrs.slnx -c Release --output ./artifacts/nupkgs

clean: ## Remove build artefacts (bin/obj/artifacts)
	dotnet clean Cqrs.slnx
	rm -rf ./artifacts

format: ## Format source with dotnet format
	dotnet format Cqrs.slnx
