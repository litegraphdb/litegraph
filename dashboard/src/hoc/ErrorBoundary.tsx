import FallBack from '@/components/base/fallback/FallBack';
import React, { ErrorInfo } from 'react';

interface Props {
  children: React.ReactNode;
  errorComponent?: (errorMessage?: string) => React.ReactNode;
  allowRefresh?: boolean;
}

interface State {
  hasError: boolean;
  errMessage?: string;
}

export default class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // eslint-disable-next-line no-console
    console.error('ErrorBoundary caught an error: ', error, errorInfo);
    this.setState({ hasError: true, errMessage: error.message });
  }

  render() {
    const { errorComponent } = this.props;
    if (this.state.hasError) {
      return errorComponent ? (
        errorComponent(this.state.errMessage)
      ) : (
        <FallBack>
          Unexpected Error: {this.state.errMessage}.
          {this.props.allowRefresh && (
            <div className="text-center mt">
              <a className="text-link" href={window.location.href} rel="noreferrer">
                Reload page
              </a>
            </div>
          )}
        </FallBack>
      );
    }

    return this.props.children;
  }
}
