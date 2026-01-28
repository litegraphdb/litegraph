import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { createMockInitialState } from '../../store/mockStore';
import { renderWithRedux } from '@/tests/store/utils';
import { getServer } from '@/tests/server';
import { handlers } from './handler';
import { commonHandlers } from '@/tests/handler';
import userEvent from '@testing-library/user-event';
import { setEndpoint, useValidateConnectivity } from '@/lib/sdk/litegraph.service';
import { useGenerateTokenMutation, useGetTenantsForEmailMutation } from '@/lib/store/slice/slice';
import { useCredentialsToLogin } from '@/hooks/authHooks';
import toast from 'react-hot-toast';
import { mockTenantData, mockToken } from '../mockData';
import LoginPage from '@/page/login/LoginPage';

const server = getServer([...commonHandlers, ...handlers]);

jest.mock('@/lib/sdk/litegraph.service', () => ({
  setEndpoint: jest.fn().mockResolvedValue(true),
  useValidateConnectivity: jest.fn().mockResolvedValue(true),
}));

// jest.mock('@/hooks/authHooks', () => ({
//   useCredentialsToLogin: jest.fn(),
// }));

// jest.mock('@/lib/store/slice/slice', () => ({
//   useGenerateTokenMutation: jest.fn(),
//   useGetTenantsForEmailMutation: jest.fn(),
// }));

// jest.mock('react-hot-toast', () => ({
//   error: jest.fn(),
//   success: jest.fn(),
// }));

// jest.mock('@/constants/constant', () => ({
//   localStorageKeys: {
//     token: 'token',
//     tenant: 'tenant',
//     serverUrl: 'serverUrl',
//   },
// }));

// // Mock UI components
// jest.mock('@/components/base/input/Input', () => {
//   return function MockLitegraphInput(props: any) {
//     return <input data-testid="litegraph-input" {...props} />;
//   };
// });

// jest.mock('@/components/base/select/Select', () => {
//   return function MockLitegraphSelect(props: any) {
//     return (
//       <select
//         data-testid="litegraph-select"
//         disabled={props.disabled}
//         onChange={(e) => props.onChange?.(e.target.value)}
//         {...props}
//       >
//         {props.options?.map((option: any) => (
//           <option key={option.value} value={option.value}>
//             {option.label}
//           </option>
//         ))}
//       </select>
//     );
//   };
// });

// jest.mock('@/components/base/button/Button', () => {
//   return function MockLitegraphButton(props: any) {
//     return (
//       <button
//         data-testid="litegraph-button"
//         disabled={props.loading || props.disabled}
//         {...props}
//       >
//         {props.children}
//       </button>
//     );
//   };
// });

// jest.mock('@/components/base/flex/Flex', () => {
//   return function MockLitegraphFlex(props: any) {
//     return <div data-testid="litegraph-flex" {...props}>{props.children}</div>;
//   };
// });

// jest.mock('@/components/layout/LoginLayout', () => {
//   return function MockLoginLayout(props: any) {
//     return <div data-testid="login-layout">{props.children}</div>;
//   };
// });

// jest.mock('@/theme/theme', () => ({
//   LightGraphTheme: {
//     primary: '#1890ff',
//   },
// }));

// // Mock localStorage
// const mockLocalStorage = {
//   getItem: jest.fn(),
//   setItem: jest.fn(),
//   removeItem: jest.fn(),
//   clear: jest.fn(),
// };
// Object.defineProperty(window, 'localStorage', {
//   value: mockLocalStorage,
// });

describe('LoginPage', () => {
  beforeAll(() => {
    server.listen();
  });
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('should render the LoginPage', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<LoginPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByText('LiteGraph Server URL')).toBeVisible();
      expect(screen.getByPlaceholderText('https://your-litegraph-server.com')).toBeVisible();
    });
    expect(container).toMatchSnapshot('initial login step');

    const nextButton = screen.getByRole('button', { name: /next/i });
    await waitFor(() => {
      expect(nextButton).toBeVisible();
    });
  });

  it('should display server URL input field', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<LoginPage />, initialState, undefined, true);

    const urlInput = screen.getByPlaceholderText('https://your-litegraph-server.com');
    await waitFor(() => {
      expect(urlInput).toBeVisible();
    });
    expect(urlInput).toMatchSnapshot();
  });

  describe('Initial Render and Step 0 (Server URL)', () => {
    it('should render login page with initial step (server URL)', () => {
      const initialState = createMockInitialState();
      renderWithRedux(<LoginPage />, initialState, undefined, true);

      expect(screen.getByLabelText('LiteGraph Server URL')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('https://your-litegraph-server.com')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument();
    });

    it('should show step indicators with first step active', () => {
      const initialState = createMockInitialState();
      renderWithRedux(<LoginPage />, initialState);

      const stepIndicators = document.querySelectorAll('[class*="stepIndicator"]');
      expect(stepIndicators).toHaveLength(5);
    });

    //   it('should validate URL field and show error for invalid URL', async () => {
    //     const user = userEvent.setup();
    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage />, initialState);

    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'invalid-url');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(screen.getByText('Please enter a valid URL!')).toBeInTheDocument();
    //     });
    //   });

    //   it('should reject non-HTTP/HTTPS URLs', async () => {
    //     const user = userEvent.setup();
    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage />, initialState);

    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'ftp://example.com');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(screen.getByText('Only HTTP or HTTPS URLs are allowed!')).toBeInTheDocument();
    //     });
    //   });

    //   it('should proceed to step 1 after successful server validation', async () => {
    //     const user = userEvent.setup();
    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage />, initialState);

    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(setEndpoint).toHaveBeenCalledWith('https://example.com');
    //       expect(useValidateConnectivity).toHaveBeenCalled();
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });
    //   });

    //   it('should not proceed if server validation fails', async () => {
    //     const user = userEvent.setup();
    //     const initialState = createMockInitialState();
    //         renderWithRedux(<LoginPage />, initialState);

    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(useValidateConnectivity).toHaveBeenCalled();
    //       // Should remain on step 0
    //       expect(screen.getByLabelText('LiteGraph Server URL')).toBeInTheDocument();
    //     });
    //   });
    // });

    // describe('Step 1 (Email Input)', () => {
    //   beforeEach(async () => {
    //     // Setup to reach step 1
    //   });

    //   const navigateToStep1 = async () => {
    //     const user = userEvent.setup();
    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage />, initialState);

    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });

    //     return user;
    //   };

    //   it('should render email input on step 1', async () => {
    //     await navigateToStep1();

    //     expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     expect(screen.getByPlaceholderText('Email')).toBeInTheDocument();
    //     expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
    //     expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument();
    //   });

    //   it('should validate email field', async () => {
    //     const user = await navigateToStep1();

    //     const emailInput = screen.getByTestId('litegraph-input');
    //     await user.type(emailInput, 'invalid-email');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(screen.getByText('Please enter a valid email!')).toBeInTheDocument();
    //     });
    //   });

    //   it('should proceed to step 3 with single tenant', async () => {
    //     const user = await navigateToStep1();

    //     const emailInput = screen.getByTestId('litegraph-input');
    //     await user.type(emailInput, 'user@example.com');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(useGetTenantsForEmailMutation).toHaveBeenCalledWith('user@example.com');
    //       expect(screen.getByPlaceholderText('Password')).toBeInTheDocument();
    //     });
    //   });

    //   it('should proceed to step 2 with multiple tenants', async () => {
    //     const user = await navigateToStep1();

    //     const emailInput = screen.getByTestId('litegraph-input');
    //     await user.type(emailInput, 'user@example.com');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(useGetTenantsForEmailMutation).toHaveBeenCalledWith('user@example.com');
    //       expect(screen.getByLabelText('Tenants')).toBeInTheDocument();
    //     });
    //   });

    //   it('should handle tenant fetch error', async () => {
    //     const user = await navigateToStep1();

    //     const emailInput = screen.getByTestId('litegraph-input');
    //     await user.type(emailInput, 'user@example.com');

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //         expect(useGetTenantsForEmailMutation).toHaveBeenCalledWith('user@example.com');
    //       // Should remain on step 1
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });
    //   });
    // });

    // describe('Step 2 (Tenant Selection)', () => {
    //   const navigateToStep2 = async () => {
    //     const user = userEvent.setup();

    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage />, initialState);

    //     // Step 0: URL
    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });

    //     // Step 1: Email
    //     const emailInput = screen.getByTestId('litegraph-input');
    //     await user.type(emailInput, 'user@example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Tenants')).toBeInTheDocument();
    //     });

    //     return user;
    //   };

    //   it('should render tenant selection on step 2', async () => {
    //     await navigateToStep2();

    //     expect(screen.getByLabelText('Tenants')).toBeInTheDocument();
    //     expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    //     expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
    //     expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument();
    //   });

    //   it('should display tenant options', async () => {
    //     await navigateToStep2();

    //     const selectElement = screen.getByTestId('litegraph-select');
    //     expect(selectElement).toBeInTheDocument();

    //     const options = screen.getAllByRole('option');
    //     expect(options).toHaveLength(mockTenantData.length);
    //     expect(screen.getByText('Tenant One')).toBeInTheDocument();
    //     expect(screen.getByText('Tenant Two')).toBeInTheDocument();
    //   });

    //   it('should proceed to step 3 after tenant selection', async () => {
    //     const user = await navigateToStep2();

    //     const selectElement = screen.getByTestId('litegraph-select');
    //     fireEvent.change(selectElement, { target: { value: 'tenant-1' } });

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(screen.getByPlaceholderText('Password')).toBeInTheDocument();
    //     });
    //   });

    //   it('should validate tenant selection is required', async () => {
    //     const user = await navigateToStep2();

    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(screen.getByText('Please select a tenant!')).toBeInTheDocument();
    //     });
    //   });
    // });

    // describe('Step 3 (Password and Login)', () => {
    //   const navigateToStep3 = async () => {
    //     const user = userEvent.setup();

    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage />, initialState);

    //     // Navigate through steps
    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });

    //     const emailInput = screen.getByTestId('litegraph-input');
    //     await user.type(emailInput, 'user@example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     await waitFor(() => {
    //       expect(screen.getByPlaceholderText('Password')).toBeInTheDocument();
    //     });

    //     return user;
    //   };

    //   it('should render password input on step 3', async () => {
    //     await navigateToStep3();

    //     expect(screen.getByPlaceholderText('Password')).toBeInTheDocument();
    //     expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
    //     expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument();
    //   });

    //   it('should validate password field is required', async () => {
    //     const user = await navigateToStep3();

    //     const loginButton = screen.getByRole('button', { name: /login/i });
    //     await user.click(loginButton);

    //     await waitFor(() => {
    //       expect(screen.getByText('Please input your password!')).toBeInTheDocument();
    //     });
    //   });

    //   it('should handle successful login', async () => {
    //     const user = await navigateToStep3();

    //     const passwordInput = screen.getByPlaceholderText('Password');
    //     await user.type(passwordInput, 'password123');

    //     const loginButton = screen.getByRole('button', { name: /login/i });
    //     await user.click(loginButton);

    //     await waitFor(() => {
    //       expect(useGenerateTokenMutation).toHaveBeenCalledWith({
    //         email: 'user@example.com',
    //         password: 'password123',
    //         tenantId: 'tenant-1',
    //       });
    //       expect(localStorage.setItem).toHaveBeenCalledWith('token', JSON.stringify(mockToken));
    //       expect(localStorage.setItem).toHaveBeenCalledWith('tenant', JSON.stringify(mockTenantData[0]));
    //       expect(localStorage.setItem).toHaveBeenCalledWith('serverUrl', 'https://example.com');
    //       expect(useCredentialsToLogin).toHaveBeenCalledWith(mockToken, mockTenantData[0]);
    //     });
    //   });

    //   it('should handle login error when tenant not found', async () => {
    //     const user = await navigateToStep3();

    //     // Mock scenario where tenant is not found

    //     const passwordInput = screen.getByPlaceholderText('Password');
    //     await user.type(passwordInput, 'password123');

    //     const loginButton = screen.getByRole('button', { name: /login/i });
    //     await user.click(loginButton);

    //     await waitFor(() => {
    //       expect(toast.error).toHaveBeenCalledWith('Tenant not found');
    //     });
    //   });

    //   it('should handle token generation failure', async () => {
    //     const user = await navigateToStep3();

    //     const passwordInput = screen.getByPlaceholderText('Password');
    //     await user.type(passwordInput, 'password123');

    //     const loginButton = screen.getByRole('button', { name: /login/i });
    //     await user.click(loginButton);

    //     await waitFor(() => {
    //       expect(useGenerateTokenMutation).toHaveBeenCalled();
    //       // Should handle error gracefully
    //     });
    //   });
    // });

    // describe('Navigation', () => {
    //   it('should allow going back from step 1 to step 0', async () => {
    //     const user = userEvent.setup();

    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage />, initialState);

    //     // Navigate to step 1
    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });

    //     // Go back to step 0
    //     const backButton = screen.getByRole('button', { name: /back/i });
    //     await user.click(backButton);

    //     expect(screen.getByLabelText('LiteGraph Server URL')).toBeInTheDocument();
    //     expect(screen.queryByRole('button', { name: /back/i })).not.toBeInTheDocument();
    //   });

    //   it('should preserve form data when navigating back and forth', async () => {
    //     const user = userEvent.setup();

    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage/>, initialState);

    //     // Fill step 0
    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });

    //     // Go back and verify URL is preserved
    //     await user.click(screen.getByRole('button', { name: /back/i }));

    //     const preservedUrlInput = screen.getByTestId('litegraph-input');
    //     expect(preservedUrlInput).toHaveValue('https://example.com');
    //   });
    // });

    // describe('Loading States', () => {
    //   it('should show loading state during server validation', async () => {
    //     const user = userEvent.setup();

    //     const initialState = createMockInitialState();
    //       renderWithRedux(<LoginPage />, initialState);

    //     const urlInput = screen.getByTestId('litegraph-input');
    //     expect(urlInput).toBeDisabled();

    //     const nextButton = screen.getByRole('button', { name: /loading/i });
    //     expect(nextButton).toBeDisabled();
    //   });

    //   it('should show loading state during tenant fetch', async () => {

    //     const initialState = createMockInitialState();
    //       renderWithRedux(<LoginPage />, initialState);

    //     const nextButton = screen.getByRole('button', { name: /loading/i });
    //     expect(nextButton).toBeDisabled();
    //   });

    //   it('should show loading state during token generation', async () => {

    //     const initialState = createMockInitialState();
    //       renderWithRedux(<LoginPage />, initialState);

    //     const loginButton = screen.getByRole('button', { name: /loading/i });
    //     expect(loginButton).toBeDisabled();
    //   });
    // });

    // describe('Form Validation Edge Cases', () => {
    //   it('should handle form validation errors gracefully', async () => {
    //     const user = userEvent.setup();
    //     const initialState = createMockInitialState();
    //         renderWithRedux(<LoginPage />, initialState);

    //     // Try to proceed without filling required field
    //     const nextButton = screen.getByRole('button', { name: /next/i });
    //     await user.click(nextButton);

    //     await waitFor(() => {
    //       expect(screen.getByText('Please enter the LiteGraph Server URL!')).toBeInTheDocument();
    //     });
    //   });

    //   it('should handle empty tenant response', async () => {
    //     const user = userEvent.setup();

    //     const initialState = createMockInitialState();
    //     renderWithRedux(<LoginPage/>, initialState);

    //     // Navigate to step 1
    //     const urlInput = screen.getByTestId('litegraph-input');
    //     await user.type(urlInput, 'https://example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });

    //     const emailInput = screen.getByTestId('litegraph-input');
    //     await user.type(emailInput, 'user@example.com');
    //     await user.click(screen.getByRole('button', { name: /next/i }));

    //     // Should remain on step 1 since no tenants found
    //     await waitFor(() => {
    //       expect(screen.getByLabelText('Email')).toBeInTheDocument();
    //     });
    //   });
  });
});
