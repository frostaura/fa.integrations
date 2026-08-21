.DEFAULT_GOAL := help

BACKEND := src/backend
API     := $(BACKEND)/FrostAura.Integrations.Api
PORT    ?= 5199

# The workload resolver crashes on this checkout (the tree lives under an
# iCloud-synced path). Nothing here uses workloads, so turn it off for every
# target rather than leaving each developer to discover the stack trace.
export MSBuildEnableWorkloadResolver = false
export DOTNET_CLI_TELEMETRY_OPTOUT = 1
export DOTNET_NOLOGO = 1

.PHONY: help build test run up down lint clean smoke

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-8s\033[0m %s\n", $$1, $$2}'

build: ## Restore and build the solution
	dotnet build $(BACKEND)

test: ## Run the test suite
	dotnet test $(BACKEND)

run: ## Run the API locally on $(PORT)
	ASPNETCORE_URLS=http://localhost:$(PORT) dotnet run --project $(API) --no-launch-profile

lint: ## Verify formatting
	dotnet format $(BACKEND) --verify-no-changes

up: ## Build and start the container stack
	docker compose up --build -d

down: ## Stop the container stack
	docker compose down

clean: ## Remove build output
	rm -rf $(BACKEND)/*/bin $(BACKEND)/*/obj

smoke: ## Hit the running API's read-only endpoints
	@curl -sf http://localhost:$(PORT)/api/health && echo
	@curl -sf http://localhost:$(PORT)/api/integrations >/dev/null && echo "integrations: ok"
	@curl -sf "http://localhost:$(PORT)/api/woolworths/search?q=baby+spinach&limit=1" >/dev/null && echo "search: ok"
