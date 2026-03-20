.DEFAULT_GOAL := help

.PHONY: help build test

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-10s\033[0m %s\n", $$1, $$2}'

build: ## Build the solution
	dotnet build Cqrs.slnx

test: ## Run all tests
	dotnet test Cqrs.slnx --logger "console;verbosity=normal"
