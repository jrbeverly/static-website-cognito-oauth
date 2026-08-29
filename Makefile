SHELL := bash
.SHELLFLAGS := -euo pipefail -c
.DEFAULT_GOAL := help

TF := terraform -chdir=env/my-sites/staging

.PHONY: help build deploy e2e destroy clean

help: ## List targets
	@grep -E '^[a-z0-9-]+:.*## ' $(MAKEFILE_LIST) | awk -F':.*## ' '{printf "  %-8s %s\n", $$1, $$2}'

build: ## Package the .NET 8 Lambda into build/publishing-api.zip
	dotnet publish src/PublishingApi/PublishingApi.Api/PublishingApi.Api.csproj -c Release -r linux-arm64 --self-contained false -o build/publish
	rm -f build/publishing-api.zip
	cd build/publish && zip -qr ../publishing-api.zip .

deploy: build ## terraform init + apply the staging environment (~3 min, CloudFront)
	$(TF) init -input=false
	$(TF) apply -input=false -auto-approve

e2e: ## Token, create site, publish a ZIP, fetch it through CloudFront, delete
	scripts/e2e.sh

destroy: ## terraform destroy (~3.5 min)
	$(TF) destroy -input=false -auto-approve

clean: ## Remove build output
	rm -rf build
