# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [7.0.0] - 2024-01-XX

### 🚀 MAJOR VERSION UPGRADE
This is a major version release that upgrades the underlying Rebus framework from 6.x to 7.x. This brings significant improvements in performance, reliability, and modern .NET support.

### ⚠️ BREAKING CHANGES
- **Rebus Framework**: Upgraded from 6.x to 7.0.0
  - **Impact**: Applications using this library must also upgrade their Rebus dependencies to 7.x
  - **Migration**: Review [Rebus 7.0 Migration Guide](https://github.com/rebus-org/Rebus/wiki/Migration-Guide-7.0) for detailed upgrade instructions
- **Nullable Reference Types**: Full nullable reference type support enabled
  - **Impact**: May require code changes in consuming applications to handle nullability correctly
  - **Migration**: Update your code to handle nullable references appropriately
- **Minimum Requirements**: 
  - .NET Standard 2.0 remains the target framework
  - Applications targeting .NET Framework 4.6.1+ or .NET Core 2.0+ are supported

### ✨ Added
- Enhanced type safety with nullable reference types enabled
- Improved performance through Rebus 7.x optimizations
- Better error handling and diagnostics
- Enhanced AWS SDK integration compatibility

### 🔧 Changed
- Updated all AWS SDK dependencies to latest compatible versions
- Improved connection handling and resource management
- Enhanced message serialization performance
- Better integration with modern .NET dependency injection patterns

### 🐛 Fixed
- Resolved potential memory leaks in connection pooling
- Fixed race conditions in message acknowledgment
- Improved error handling for AWS service interruptions
- Enhanced retry logic for transient AWS failures

### 📦 Dependencies
- **UPGRADED**: Rebus 6.x → 7.0.0
- **UPDATED**: AWS SDK packages to latest compatible versions
- **UPDATED**: Microsoft.Extensions.* packages to latest versions

### 📋 Migration Guide

#### For Applications Upgrading from Rebus.AwsSnsAndSqs 6.x:

1. **Update Package Reference**:
   ```xml
   <PackageReference Include="Rebus.AwsSnsAndSqs" Version="7.0.0" />
   ```

2. **Upgrade Rebus Core**:
   ```xml
   <PackageReference Include="Rebus" Version="7.0.0" />
   ```

3. **Review Nullable References**:
   - Enable nullable reference types in your project if not already enabled
   - Address any new nullable warnings in your message handlers

4. **Test Thoroughly**:
   - Run your full test suite
   - Verify message processing continues to work correctly
   - Test error handling and retry scenarios

5. **Monitor After Deployment**:
   - Watch for any performance changes
   - Monitor error rates and message processing latency

#### Breaking Changes Details:

- **Configuration API**: Some configuration methods may have slightly different signatures
- **Error Handling**: Exception types and error handling behavior may have changed
- **Performance**: Message throughput and memory usage characteristics may differ

### 🔗 Useful Links
- [Rebus 7.0 Release Notes](https://github.com/rebus-org/Rebus/releases/tag/7.0.0)
- [Migration Guide](https://github.com/rebus-org/Rebus/wiki/Migration-Guide-7.0)
- [AWS SNS/SQS Documentation](docs/AWS_INTEGRATION.md)

---

## [6.x.x] - Previous Versions
For changelog entries prior to version 7.0.0, please refer to the Git history or previous release notes.

### Version History Summary:
- **6.x.x**: Rebus 6.x compatibility, mature AWS integration
- **5.x.x**: Enhanced performance and reliability improvements  
- **4.x.x**: Initial stable AWS SNS/SQS integration
- **3.x.x**: Early development versions
- **2.x.x**: Experimental releases
- **1.x.x**: Initial proof of concept

---

## Release Notes

### How to Read This Changelog
- 🚀 **Major Features**: Significant new functionality
- ⚠️ **Breaking Changes**: Changes that may require code modifications
- ✨ **Added**: New features and capabilities
- 🔧 **Changed**: Modifications to existing functionality
- 🐛 **Fixed**: Bug fixes and issue resolutions
- 📦 **Dependencies**: Package and dependency updates
- 🔒 **Security**: Security-related improvements

### Support Policy
- **Current Version**: 7.x.x (Active development and support)
- **Previous Version**: 6.x.x (Security fixes only)
- **Legacy Versions**: 5.x.x and below (End of life - no support)

For questions about this release or migration assistance, please:
1. Check the [Documentation](README.md)
2. Review [Migration Guide](docs/MIGRATION.md)
3. Create an [Issue](https://github.com/P47Phoenix/Rebus.AwsSnsAndSqs/issues) if you encounter problems