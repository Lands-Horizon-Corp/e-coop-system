# Linux Testing Checklist for ECoopSystem

## Pre-Testing Setup

### Environment Information
- [ ] Linux Distribution: _______________
- [ ] Kernel Version: `uname -r`
- [ ] Desktop Environment: _______________
- [ ] Display Server: X11 / Wayland
- [ ] .NET Version: `dotnet --version`

### Dependencies Installed
- [ ] .NET 9 SDK/Runtime
- [ ] libX11 and X11 libraries
- [ ] GTK3
- [ ] CEF dependencies (for WebView)
- [ ] SSL/TLS certificates updated

## Build Testing

### Compilation
- [ ] `dotnet restore` completes without errors
- [ ] `dotnet build` completes successfully
- [ ] No Windows-specific warnings/errors
- [ ] All projects target net9.0

### Publishing
- [ ] Framework-dependent publish works
  ```bash
  dotnet publish -c Release -r linux-x64 --no-self-contained
  ```
- [ ] Self-contained publish works
  ```bash
  dotnet publish -c Release -r linux-x64 --self-contained
  ```
- [ ] Single-file publish works
  ```bash
  dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
  ```
- [ ] Executable has correct permissions (755)

## Runtime Testing

### Application Startup
- [ ] Application launches without errors
- [ ] No missing library errors (`ldd` check passes)
- [ ] Window appears correctly
- [ ] System decorations work properly
- [ ] Application icon displays (if configured)

### File System Operations
- [ ] Config directory created: `~/.config/ECoopSystem/`
- [ ] Data protection keys directory created
- [ ] Files have correct permissions (600 for .dat files)
- [ ] Can read/write appstate.dat
- [ ] Can read/write secret.dat

### Machine ID Detection
- [ ] Machine ID successfully retrieved
- [ ] Falls back to `/var/lib/dbus/machine-id` if needed
- [ ] Uses `Environment.MachineName` as last resort
- [ ] Consistent across application restarts

### Network Operations
- [ ] HTTP requests work (license validation)
- [ ] SSL/TLS certificates validated correctly
- [ ] Timeout handling works
- [ ] Error handling for network failures
- [ ] Development SSL warnings appear in debug mode

### License Activation
- [ ] Activation view displays correctly
- [ ] Can enter license key
- [ ] HTTP POST request to activation endpoint works
- [ ] License stored securely in `secret.dat`
- [ ] License persists across restarts

### License Verification
- [ ] Verification on startup works
- [ ] Grace period handling works when offline
- [ ] Invalid license redirects to activation
- [ ] Counter increments correctly
- [ ] LastVerifiedUtc updates properly

### WebView Functionality
- [ ] WebView control initializes
- [ ] CEF libraries load correctly
- [ ] Configured URL loads
- [ ] JavaScript execution works
- [ ] SSL certificates validated in WebView
- [ ] Trusted domain validation works
- [ ] No console errors in WebView

### Window Management
- [ ] Locked mode (activation view):
  - [ ] Fixed window size
  - [ ] No resize handles
  - [ ] No system decorations
  - [ ] Close button works
- [ ] Normal mode (main view):
  - [ ] Resizable window
  - [ ] System decorations present
  - [ ] Minimize/maximize works
  - [ ] Fullscreen works (if used)

### URL Opening
- [ ] Social media links open in browser
- [ ] `xdg-open` command works
- [ ] HTTP/HTTPS URLs open
- [ ] Mailto links work
- [ ] Invalid URLs blocked
- [ ] Localhost blocked in production

### Data Protection
- [ ] Keys persisted to filesystem
- [ ] Encryption/decryption works
- [ ] Protected data survives restart
- [ ] Tampering detection works
- [ ] Key rotation works (if implemented)

### Configuration Loading
- [ ] appsettings.json loaded correctly
- [ ] appsettings.Development.json overrides work
- [ ] Environment-specific settings apply
- [ ] Configuration validation works

## Graphics & UI Testing

### Display Rendering
- [ ] UI renders correctly on X11
- [ ] UI renders correctly on Wayland
- [ ] Hardware acceleration works (if available)
- [ ] Fonts render properly (Inter font)
- [ ] Icons display correctly
- [ ] Colors match design

### HiDPI/Scaling
- [ ] Application scales correctly on HiDPI displays
- [ ] Text is crisp and readable
- [ ] No blurry UI elements
- [ ] Scaling factor detected correctly

### Desktop Environment Specific
- [ ] GNOME: Works correctly
- [ ] KDE Plasma: Works correctly
- [ ] XFCE: Works correctly
- [ ] Cinnamon: Works correctly
- [ ] MATE: Works correctly

## Error Handling

### Graceful Failures
- [ ] Missing machine-id handled
- [ ] Network unavailable handled
- [ ] Invalid config handled
- [ ] Corrupted data files handled
- [ ] WebView init failure handled

### Logging
- [ ] Debug logs appear in console
- [ ] Error messages are helpful
- [ ] No sensitive data in logs
- [ ] SSL warnings visible in debug

## Performance Testing

### Startup Time
- [ ] Cold start: _____ seconds
- [ ] Warm start: _____ seconds
- [ ] WebView load: _____ seconds

### Memory Usage
- [ ] Initial memory: _____ MB
- [ ] After 5 minutes: _____ MB
- [ ] After 30 minutes: _____ MB
- [ ] No memory leaks detected

### Resource Usage
- [ ] CPU usage acceptable
- [ ] GPU usage acceptable (if using acceleration)
- [ ] No excessive disk I/O
- [ ] Network usage reasonable

## Platform-Specific Issues

### Known Linux Issues
- [ ] Document any Wayland-specific issues
- [ ] Document any X11-specific issues
- [ ] Note desktop environment incompatibilities
- [ ] List any required workarounds

### Distribution-Specific
- [ ] Ubuntu-specific issues: _______________
- [ ] Fedora-specific issues: _______________
- [ ] Arch-specific issues: _______________
- [ ] Other distro issues: _______________

## Security Testing

### File Permissions
- [ ] Config files not world-readable
- [ ] Executable has safe permissions
- [ ] No temporary files left behind
- [ ] Log files have correct permissions

### Network Security
- [ ] Only HTTPS in production
- [ ] Certificate pinning works (if used)
- [ ] No plaintext secrets in memory dumps
- [ ] Firewall rules documented

## Cleanup Testing

### Uninstall
- [ ] Application can be removed cleanly
- [ ] Config directory can be deleted
- [ ] No orphaned processes
- [ ] No leftover files in system directories

## Automated Testing

### Unit Tests
- [ ] All unit tests pass on Linux
- [ ] Platform-specific tests pass
- [ ] No flaky tests

### Integration Tests
- [ ] Integration tests pass
- [ ] Cross-platform tests pass

## Documentation

### User Documentation
- [ ] README.md covers Linux
- [ ] LINUX.md has all necessary info
- [ ] Installation steps clear
- [ ] Troubleshooting guide complete

### Developer Documentation
- [ ] Build instructions work
- [ ] Dependencies documented
- [ ] Platform differences noted

## Release Checklist

### Before Release
- [ ] All tests pass
- [ ] Performance acceptable
- [ ] No critical bugs
- [ ] Documentation complete
- [ ] Build scripts work
- [ ] Example configs provided

### Release Artifacts
- [ ] Self-contained binary
- [ ] Framework-dependent binary (optional)
- [ ] SHA256 checksums
- [ ] Installation instructions
- [ ] Release notes

## Notes and Issues

```
[Document any issues found during testing here]

Example:
- WebView crashes on Wayland with NVIDIA drivers
  Workaround: Set GDK_BACKEND=x11

- High memory usage after 1 hour
  Status: Investigating
```

## Testing Sign-off

- Tested by: _______________
- Date: _______________
- Distribution: _______________
- Version: _______________
- Status: PASS / FAIL / PARTIAL
- Notes: _______________
