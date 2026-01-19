# Use the official .NET 8 SDK image as our base image
# AS base: This creates a named build stage for multi-stage builds
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base

# Set the frontend to noninteractive to prevent prompts during package installation
# This is crucial for automated Docker builds that cannot accept user input
# Without this, apt-get may hang waiting for user input during package configuration
ENV DEBIAN_FRONTEND=noninteractive


# -----------------------------------------------------------------------------
# Stage: update-packages
# -----------------------------------------------------------------------------

# Update the package index to get the latest package information
# Upgrade all installed packages to their latest versions
# Clean up apt after the operation to reduce image size
FROM base AS update-packages
RUN apt-get update \
    && apt-get upgrade -y \
    && apt clean \
    && rm -rf /var/lib/apt/lists/*


# -----------------------------------------------------------------------------
# Stage: install-packages
# -----------------------------------------------------------------------------

# Install all required development tools and dependencies
FROM update-packages AS install-packages

# Copy the package list file into the container
# This file contains all packages with their descriptions for easy maintenance
COPY packages.list /tmp/packages.list

# Install essential packages, development tools, and system utilities
# The package list is read from packages.list, filtering out comments and blank lines
RUN apt-get update \
        # apt-get update: Refreshes the package index to get the latest package information
        \
    && grep -v '^#' /tmp/packages.list | grep -v '^$' | awk '{print $1}' | xargs apt-get install -y --no-install-recommends \
        # grep -v '^#': Filters out lines starting with # (comments) from the package list
        # grep -v '^$': Filters out empty lines from the package list
        # awk '{print $1}': Extracts only the first column (package name), ignoring inline comments
        # xargs: Converts the list of packages into command-line arguments for apt-get
        # --no-install-recommends: Installs only required packages, not recommended ones, to minimize image size
        \
    && rm /tmp/packages.list \
        # deletes the temporary package list file to keep the image clean
        \
    && apt clean \
    && rm -rf /var/lib/apt/lists/*
        # Cleanup to reduce image size


# -----------------------------------------------------------------------------
# Stage: install-zscaler
# -----------------------------------------------------------------------------

# Install ZScaler root certificates to enable SSL/TLS connections through corporate proxy
# ZScaler performs SSL inspection, so its root certificate must be trusted by the system
FROM install-packages AS install-zscaler

# # Copy ZScaler certificate files from the build context to a temporary location
# COPY ca-certificates/* /usr/local/share/ca-certificates

# # Rebuild the system's certificate store to include the new ZScaler certificates
# # This updates /etc/ssl/certs/ca-certificates.crt with the newly installed certificates
# RUN update-ca-certificates

# Set Node.js environment variable to trust system certificates
# This is critical for VS Code's extension host and any Node.js processes
# Without this, Node.js will not trust ZScaler-signed certificates
ENV NODE_EXTRA_CA_CERTS=/etc/ssl/certs/ca-certificates.crt


# -----------------------------------------------------------------------------
# Stage: user-setup
# -----------------------------------------------------------------------------

# Configure a non-root user for running the container
# This follows security best practices by avoiding unnecessary root privileges
FROM install-zscaler AS user-setup

# Create a non-root user for enhanced security
# Running containers as root is a security risk; this follows the principle of least privilege
# These build arguments allow customization at build time while providing sensible defaults
ARG USER_NAME=vscode        # Default username (VS Code devcontainer convention)
ARG USER_UID=1000           # Default user ID (VS Code will override this to match host user for file permissions)
ARG USER_GID=$USER_UID      # Group ID matches user ID by default (common Linux practice for single-user scenarios)

# Create the user and group with specified UID/GID
# Also grants passwordless sudo access to the user for elevated tasks when needed
RUN groupadd --non-unique --gid $USER_GID ${USER_NAME} 2>/dev/null || true \
        # Creates a group with specific GID (ignores error if GID already exists)
        \
    && useradd --non-unique --uid $USER_UID --gid $USER_GID -m ${USER_NAME} \
        # Creates a user with specific UID/GID
        # --non-unique: Allows duplicate UIDs (safe in containers)
        # -m: creates home directory
        \
    && echo "${USER_NAME} ALL=(ALL:ALL) NOPASSWD:ALL" > /etc/sudoers.d/${USER_NAME} \
        # Grants passwordless sudo access
        \
    && chmod 0440 /etc/sudoers.d/${USER_NAME}
        # Sets secure permissions (read-only for root and group)
        # 0440 = owner read, group read, others nothing

# Define an environment variable for the user's home directory
# ENV variables persist at runtime (unlike ARG which is build-time only)
ENV USER_HOME=/home/${USER_NAME}

# Ensure the user's home directory has the correct ownership
# This prevents permission issues when the user accesses their home directory
RUN chown -R ${USER_UID}:${USER_GID} ${USER_HOME}


# ----------------------------------------------------------------------------------
# Stage: zsh-setup
# ----------------------------------------------------------------------------------

# Start a new build stage for zsh configuration
FROM user-setup AS zsh-setup

# Install Oh-My-Zsh, a popular framework for managing zsh configuration
# Oh-My-Zsh provides themes, plugins, and helpful aliases for improved shell productivity
RUN su - ${USER_NAME} -s /bin/sh -c ' \
        git clone --depth=1 https://github.com/ohmyzsh/ohmyzsh.git $HOME/.oh-my-zsh \
            # Clone only the latest commit (shallow clone) to save space and time
        && cp $HOME/.oh-my-zsh/templates/zshrc.zsh-template $HOME/.zshrc \
            # Copy the default configuration template to create .zshrc
    ' \
        # su - ${USER_NAME}: Switch to the vscode user (important for proper file ownership)
        # -s /bin/sh: Use sh shell for execution (zsh isn't configured yet)
        # $HOME variable is set automatically when switching users with su
    && chown -R ${USER_UID}:${USER_GID} ${USER_HOME}
        # Recursively change ownership of all files in home directory to ensure proper permissions

# Change the default shell to zsh for the user
RUN chsh -s /usr/bin/zsh ${USER_NAME}

# Set a variable for the path to zsh configuration file
ENV SHELL_CONFIG=${USER_HOME}/.zshrc


# ----------------------------------------------------------------------------------
# Stage: finalization
# ----------------------------------------------------------------------------------

# Final build stage that represents the complete container image
# This stage inherits all previous installations and configurations
FROM zsh-setup AS finalization

# Change ownership of the user's home directory and all its contents
# This ensures the non-root user has full access to their home directory
# -R: Recursive, applies to all files and subdirectories
RUN chown -R ${USER_NAME}:${USER_NAME} ${USER_HOME}

# Switch from root to the non-root user for all subsequent commands
# All processes in the container will run as this user by default
# This is a critical security measure to prevent privilege escalation
USER ${USER_NAME}

# Set the default working directory for subsequent commands
# /workspace is a common convention for VS Code devcontainers
# This will be the directory you see when you open a terminal in the container
WORKDIR /workspace
