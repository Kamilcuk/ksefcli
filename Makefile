.PHONY: all build clean run
null  :=
space := $(null) #
comma := ,
join_comma = $(subst $(space),$(comma),$(1))

all: build

###############################################################################
S = src/KCKSeFCli
SOURCES := $(shell find $(S) \( -path $(S)/obj -o -path $(S)/bin \) -prune -o \( -type f \( -name '*.cs' -o -name '*.csproj' \) -print \) )
B = $(S)/obj
$(B)/init: ./.gitmodules
	git submodule update --init --recursive
	@mkdir -p $(dir $@) && touch $@
$(B)/build: $(B)/init $(SOURCES)
	dotnet build
	@mkdir -p $(dir $@) && touch $@
$(B)/format: $(SOURCES)
	dotnet format -v d
	@mkdir -p $(dir $@) && touch $@
###############################################################################

.PHONY: build format run test clean sources
build: $(B)/build
format: $(B)/format
sources:
	@echo $(SOURCES)
run: build
	dotnet run --project $(S) --
test: format build
	dotnet test tests/KCKSeFCli.Tests/KCKSeFCli.Tests.csproj
clean:
	dotnet clean $(S)
test-format:
	dotnet format $(S) -v d --verify-no-changes

###############################################################################

GITLAB_BUILD_CMD := $(shell sed -n '/.*- \(dotnet publish\)/{s//\1/;p;q}' .gitlab-ci.yml)
build-static:
	$(GITLAB_BUILD_CMD)
docker-build-static:
	docker run -ti --rm -u "$(shell id -u):$(shell id -g)" -v $(CURDIR):$(CURDIR) -w $(CURDIR) \
		mcr.microsoft.com/dotnet/sdk:10.0 $(GITLAB_BUILD_CMD)

.PHONY: nix-fix
nix-fix:
	for f in $$(find src/KCKSeFCli/bin dist out out-self -type f -executable -name kcksefcli 2>/dev/null); do \
		echo "Patching $$f..."; \
		patchelf --remove-rpath "$$f"; \
		patchelf --set-interpreter /lib64/ld-linux-x86-64.so.2 "$$f"; \
	done
