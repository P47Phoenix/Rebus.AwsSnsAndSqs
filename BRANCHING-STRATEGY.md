# Branching Strategy for Rebus.AwsSnsAndSqs

## Overview

This document outlines the branching strategy for the Rebus.AwsSnsAndSqs project, which is designed to align with Rebus major version releases while maintaining a clean and manageable repository structure.

## Branch Structure

### Primary Branches

#### Master Branch
- **Purpose**: Contains the latest stable release
- **Target**: Currently Rebus 7.x (latest major version)
- **Protection**: Protected branch requiring PR reviews
- **Deployment**: Automatically deployed to production/package feeds

#### Rebus Version Support Branches
- **Pattern**: `rebus-{major}-support`
- **Purpose**: Long-lived branches for each Rebus major version
- **Current Branches**:
  - `rebus-7-support`: Current development branch (active)
  - `rebus-6-support`: Maintenance branch for legacy support
- **Future Branches**:
  - `rebus-8-support`: Will be created when Rebus 8.x is released

### Feature Branches

#### Development Branches
- **Pattern**: `feature/{feature-name}`
- **Purpose**: New feature development
- **Base**: Created from appropriate `rebus-{major}-support` branch
- **Lifecycle**: Merged back to support branch via PR, then deleted

#### Bug Fix Branches
- **Pattern**: `bugfix/{bug-description}`
- **Purpose**: Non-critical bug fixes
- **Base**: Created from appropriate `rebus-{major}-support` branch
- **Lifecycle**: Merged back to support branch via PR, then deleted

#### Hotfix Branches
- **Pattern**: `hotfix/{critical-fix}`
- **Purpose**: Critical production fixes
- **Base**: Created from `master` branch
- **Lifecycle**: Merged to both `master` and relevant support branches

## Branching Workflow

### Visual Representation

```mermaid
gitgraph
    commit id: "Initial"
    branch rebus-6-support
    checkout rebus-6-support
    commit id: "Rebus 6.x Support"
    commit id: "6.x Features"
    checkout main
    merge rebus-6-support
    commit id: "Release 6.x"
    branch rebus-7-support
    checkout rebus-7-support
    commit id: "Rebus 7.x Upgrade"
    commit id: "7.x Features"
    checkout main
    merge rebus-7-support
    commit id: "Release 7.x"
    branch rebus-8-support
    checkout rebus-8-support
    commit id: "Rebus 8.x Support"
```

### Workflow Steps

#### 1. Feature Development
```bash
# Create feature branch from appropriate support branch
git checkout rebus-7-support
git pull origin rebus-7-support
git checkout -b feature/new-sns-feature

# Develop feature
# ... make changes ...
git add .
git commit -m "Add new SNS feature"

# Push and create PR
git push origin feature/new-sns-feature
# Create PR to rebus-7-support
```

#### 2. Bug Fixes
```bash
# Create bugfix branch
git checkout rebus-7-support
git pull origin rebus-7-support
git checkout -b bugfix/fix-message-serialization

# Fix bug
# ... make changes ...
git add .
git commit -m "Fix message serialization issue"

# Push and create PR
git push origin bugfix/fix-message-serialization
# Create PR to rebus-7-support
```

#### 3. Hotfixes
```bash
# Create hotfix branch from master
git checkout master
git pull origin master
git checkout -b hotfix/critical-security-fix

# Fix critical issue
# ... make changes ...
git add .
git commit -m "Fix critical security vulnerability"

# Push and create PR to master
git push origin hotfix/critical-security-fix
# Create PR to master

# After merging to master, also merge to support branches
git checkout rebus-7-support
git merge master
git push origin rebus-7-support
```

## Version Management

### Semantic Versioning

The project follows semantic versioning (MAJOR.MINOR.PATCH):
- **MAJOR**: Aligned with Rebus major version (e.g., 7.x.x for Rebus 7.x)
- **MINOR**: New features, backwards compatible
- **PATCH**: Bug fixes, backwards compatible

### Release Process

#### Regular Releases
1. **Development**: Features/bugs developed on `rebus-{major}-support` branch
2. **Testing**: Comprehensive testing on support branch
3. **Release Branch**: Create `release/{version}` from support branch
4. **Finalization**: Final testing and version bumping
5. **Merge to Master**: Merge release branch to master
6. **Tagging**: Create git tag with version number
7. **Package Publishing**: Publish to NuGet/package feeds

#### Hotfix Releases
1. **Critical Fix**: Develop on `hotfix/{fix-name}` from master
2. **Immediate Merge**: Merge directly to master
3. **Tag**: Create patch version tag
4. **Publish**: Immediate package publication
5. **Backport**: Merge to relevant support branches

## Branch Protection Rules

### Master Branch
- **Required Reviews**: 2 approvals required
- **Status Checks**: All CI/CD checks must pass
- **Up-to-date**: Branch must be up-to-date before merging
- **Admin Enforcement**: Include administrators in restrictions

### Support Branches
- **Required Reviews**: 1 approval required
- **Status Checks**: All CI/CD checks must pass
- **Up-to-date**: Branch must be up-to-date before merging

## Rebus Version Alignment

### Current Alignment
- **Master**: Rebus 7.x (latest stable)
- **rebus-7-support**: Active development for Rebus 7.x
- **rebus-6-support**: Maintenance for legacy Rebus 6.x

### Future Version Strategy
When new Rebus major versions are released:

1. **Create New Support Branch**: `rebus-{new-major}-support`
2. **Upgrade Dependencies**: Update Rebus packages
3. **Compatibility Testing**: Ensure all features work
4. **Migration Guide**: Document breaking changes
5. **Update Master**: Merge new version to master when stable

## Legacy Branch Management

### Maintenance Policy
- **Active Support**: Current major version (rebus-7-support)
- **Maintenance Mode**: Previous major version (rebus-6-support)
  - Critical security fixes only
  - No new features
  - Limited bug fixes
- **End of Life**: Versions older than 2 major releases

### Branch Cleanup
- **Feature Branches**: Deleted after merge
- **Release Branches**: Kept for historical reference
- **Hotfix Branches**: Deleted after merge
- **Support Branches**: Kept indefinitely for maintenance

## Contribution Guidelines

### For Contributors
1. **Check Current Version**: Ensure you're working with the correct support branch
2. **Feature Branches**: Always create from appropriate support branch
3. **Naming Convention**: Use descriptive branch names
4. **Commit Messages**: Follow conventional commit format
5. **Testing**: Ensure all tests pass before PR

### For Maintainers
1. **Review PRs**: Ensure alignment with target Rebus version
2. **Version Bumping**: Update version numbers appropriately
3. **Release Notes**: Maintain comprehensive changelog
4. **Documentation**: Update docs when merging version changes

## Examples

### Existing Branch Analysis
Based on current repository state:
- `master`: Current stable (Rebus 7.x)
- `Rebus-7-upgrade`: Active development branch
- `Upgrade-to-rebus-version-6`: Legacy maintenance branch
- `Make-json-serialzation-work-with-core-and-standard-framwork`: Feature branch

### Recommended Renaming
To align with this strategy:
```bash
# Rename branches to follow convention
git branch -m Rebus-7-upgrade rebus-7-support
git branch -m Upgrade-to-rebus-version-6 rebus-6-support
```

## Monitoring and Maintenance

### Regular Tasks
- **Weekly**: Review open PRs and feature branches
- **Monthly**: Cleanup merged branches
- **Quarterly**: Review support branch relevance
- **Annually**: Evaluate branch protection rules

### Automation
- **CI/CD**: Automated testing on all branches
- **Branch Cleanup**: Automated deletion of merged feature branches
- **Version Tagging**: Automated tagging on master merges
- **Package Publishing**: Automated publishing from master

---

*This branching strategy ensures clean version management aligned with Rebus major versions while maintaining development flexibility and production stability.*
