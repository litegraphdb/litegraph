# React 19 Upgrade Plan

This document outlines the steps required to upgrade the LiteGraph dashboard from React 18 to React 19.

**Current State:** React 18.3.1
**Target State:** React 19.x
**Last Updated:** 2026-02-02

---

## Pre-Upgrade Checklist

- [ ] **Backup current state**
  - [ ] Create a new git branch: `git checkout -b upgrade/react-19`
  - [ ] Commit any pending changes

- [ ] **Review React 19 breaking changes**
  - [ ] Read official migration guide: https://react.dev/blog/2024/04/25/react-19-upgrade-guide
  - [ ] Note any deprecated APIs used in codebase

- [ ] **Audit current dependencies for React 19 compatibility**
  - [ ] Run `npm ls react` to see all packages depending on React
  - [ ] Check each package for React 19 support (see Dependency Audit section below)

---

## Dependency Audit

Review each dependency for React 19 compatibility. Mark as compatible, needs update, or needs replacement.

### Core React Packages
| Package | Current | React 19 Compatible | Action Required | Status |
|---------|---------|---------------------|-----------------|--------|
| react | 18.3.1 | N/A - upgrading | Upgrade to ^19.0.0 | [ ] |
| react-dom | 18.3.1 | N/A - upgrading | Upgrade to ^19.0.0 | [ ] |
| @types/react | 18.2.64 | Check | Upgrade to ^19.0.0 | [ ] |
| @types/react-dom | 18.2.21 | Check | Upgrade to ^19.0.0 | [ ] |

### Ant Design Packages
| Package | Current | React 19 Compatible | Action Required | Status |
|---------|---------|---------------------|-----------------|--------|
| antd | 5.29.3 | Yes (with patch) | Install @ant-design/v5-patch-for-react-19 | [ ] |
| @ant-design/cssinjs | 1.24.0 | Check | May need update | [ ] |
| @ant-design/icons | 5.6.1 | Check | May need update | [ ] |
| @ant-design/nextjs-registry | 1.3.0 | Check | May need update | [ ] |

### Third-Party React Libraries
| Package | Current | React 19 Compatible | Action Required | Status |
|---------|---------|---------------------|-----------------|--------|
| jsoneditor-react | 3.1.2 | **No** | Find alternative or fork | [ ] |
| react-force-graph-3d | 1.29.0 | Check | Verify compatibility | [ ] |
| react-hot-toast | 2.6.0 | Check | Verify compatibility | [ ] |
| react-redux | 9.2.0 | Yes | Should work | [ ] |
| react-resizable | 3.1.3 | Check | Verify compatibility | [ ] |
| react-toggle-dark-mode | 1.1.1 | Check | Verify compatibility | [ ] |
| @react-sigma/core | 3.4.2 | Check | Verify compatibility | [ ] |

### Testing Libraries
| Package | Current | React 19 Compatible | Action Required | Status |
|---------|---------|---------------------|-----------------|--------|
| @testing-library/react | 16.3.2 | Yes | Should work | [ ] |
| @testing-library/jest-dom | 6.6.3 | Yes | Should work | [ ] |

---

## Upgrade Steps

### Phase 1: Prepare Environment

- [ ] **1.1 Clear npm cache and node_modules**
  ```bash
  rm -rf node_modules package-lock.json
  npm cache clean --force
  ```

- [ ] **1.2 Update package.json overrides**
  Add/update the overrides section to handle peer dependency conflicts:
  ```json
  "overrides": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "jsoneditor-react": {
      "react": "$react",
      "react-dom": "$react-dom"
    }
  }
  ```

### Phase 2: Upgrade React Core

- [ ] **2.1 Upgrade React packages**
  ```bash
  npm install react@^19.0.0 react-dom@^19.0.0
  npm install -D @types/react@^19.0.0 @types/react-dom@^19.0.0
  ```

- [ ] **2.2 Install antd React 19 patch**
  ```bash
  npm install @ant-design/v5-patch-for-react-19
  ```

- [ ] **2.3 Update AppProviders.tsx to use the patch**
  Add at the top of AppProviders.tsx:
  ```typescript
  import '@ant-design/v5-patch-for-react-19';
  ```

### Phase 3: Handle Breaking Changes

- [ ] **3.1 Update deprecated APIs**

  React 19 removes these deprecated APIs:
  - [ ] `ReactDOM.render` → use `createRoot`
  - [ ] `ReactDOM.hydrate` → use `hydrateRoot`
  - [ ] `ReactDOM.unmountComponentAtNode` → use `root.unmount()`
  - [ ] `ReactDOM.findDOMNode` → use refs
  - [ ] Legacy Context (`contextTypes`, `getChildContext`) → use `createContext`

- [ ] **3.2 Update ref handling**

  React 19 changes:
  - [ ] Refs can now be passed as regular props (no need for `forwardRef` in many cases)
  - [ ] Review components using `forwardRef` for simplification opportunities

- [ ] **3.3 Handle removed legacy APIs**
  - [ ] `propTypes` - remove if present (use TypeScript instead)
  - [ ] `defaultProps` on function components - use default parameters instead

### Phase 4: Address Problematic Dependencies

- [ ] **4.1 jsoneditor-react replacement**

  Options:
  - [ ] Option A: Use npm overrides to force React 19 (may have runtime issues)
  - [ ] Option B: Replace with `@monaco-editor/react` for JSON editing
  - [ ] Option C: Fork jsoneditor-react and update peer dependencies
  - [ ] Option D: Use vanilla jsoneditor with a React wrapper

  **Chosen approach:** ________________

- [ ] **4.2 Test react-force-graph-3d compatibility**
  - [ ] Check for React 19 compatible version
  - [ ] Test 3D graph rendering functionality
  - [ ] If incompatible, evaluate alternatives

- [ ] **4.3 Test other react-* packages**
  - [ ] react-resizable
  - [ ] react-toggle-dark-mode
  - [ ] react-hot-toast

### Phase 5: Update Configuration

- [ ] **5.1 Update Next.js configuration (if needed)**
  - [ ] Check next.config.js for React-specific settings
  - [ ] Update any experimental React features

- [ ] **5.2 Update TypeScript configuration**
  - [ ] Ensure `tsconfig.json` is compatible with React 19 types
  - [ ] Update `jsx` setting if needed

### Phase 6: Testing

- [ ] **6.1 Run build**
  ```bash
  npm run build
  ```
  - [ ] Fix any TypeScript errors
  - [ ] Fix any build warnings

- [ ] **6.2 Run unit tests**
  ```bash
  npm run test
  ```
  - [ ] Fix any failing tests
  - [ ] Update test utilities if needed

- [ ] **6.3 Manual testing checklist**

  **Authentication:**
  - [ ] User login flow (server URL → email → tenant → password)
  - [ ] Admin login flow
  - [ ] SSO login
  - [ ] Logout functionality

  **Dashboard:**
  - [ ] Dashboard home loads correctly
  - [ ] Theme switching works (light/dark)
  - [ ] Navigation works

  **CRUD Operations:**
  - [ ] Tenants: Create, Read, Update, Delete
  - [ ] Users: Create, Read, Update, Delete
  - [ ] Graphs: Create, Read, Update, Delete
  - [ ] Nodes: Create, Read, Update, Delete
  - [ ] Edges: Create, Read, Update, Delete
  - [ ] Labels: Create, Read, Update, Delete
  - [ ] Tags: Create, Read, Update, Delete
  - [ ] Vectors: Create, Read, Update, Delete

  **Special Features:**
  - [ ] JSON editor functionality (nodes, edges data fields)
  - [ ] 3D graph visualization
  - [ ] Search functionality
  - [ ] Vector index configuration
  - [ ] Backup management

- [ ] **6.4 Console warnings check**
  - [ ] No React hydration warnings
  - [ ] No antd compatibility warnings
  - [ ] No useForm connection warnings

### Phase 7: Finalize

- [ ] **7.1 Update documentation**
  - [ ] Update README.md if needed
  - [ ] Document any breaking changes for other developers

- [ ] **7.2 Clean up**
  - [ ] Remove any temporary workarounds
  - [ ] Remove unused overrides from package.json
  - [ ] Delete this file or mark as completed

- [ ] **7.3 Create pull request**
  - [ ] Write PR description with changes summary
  - [ ] Request code review
  - [ ] Merge after approval

---

## Rollback Plan

If the upgrade causes critical issues:

1. [ ] Checkout the previous stable branch
   ```bash
   git checkout main
   ```

2. [ ] Delete failed upgrade branch (optional)
   ```bash
   git branch -D upgrade/react-19
   ```

3. [ ] Document issues encountered for future reference

---

## Known Issues & Solutions

### Issue: Hydration mismatch with antd CSS-in-JS
**Solution:** Ensure `@ant-design/v5-patch-for-react-19` is imported at the app entry point.

### Issue: jsoneditor-react peer dependency conflict
**Solution:** Use npm overrides or replace with alternative JSON editor.

### Issue: useForm not connected warning
**Solution:** Ensure all `Form.useForm()` instances are connected to a `<Form form={form}>` component.

---

## Progress Tracking

| Phase | Description | Status | Completed By | Date |
|-------|-------------|--------|--------------|------|
| 1 | Prepare Environment | Not Started | | |
| 2 | Upgrade React Core | Not Started | | |
| 3 | Handle Breaking Changes | Not Started | | |
| 4 | Address Problematic Dependencies | Not Started | | |
| 5 | Update Configuration | Not Started | | |
| 6 | Testing | Not Started | | |
| 7 | Finalize | Not Started | | |

---

## Notes

_Add any notes, observations, or issues encountered during the upgrade process here._

```
Date:
Note:
```

```
Date:
Note:
```

```
Date:
Note:
```
