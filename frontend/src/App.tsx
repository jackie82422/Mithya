import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfigProvider, theme } from 'antd';
import { useTranslation } from 'react-i18next';
import zhTW from 'antd/locale/zh_TW';
import enUS from 'antd/locale/en_US';
import { ThemeProvider, useTheme } from './shared/contexts/ThemeContext';
import AppLayout from './shared/layouts/AppLayout';
import DashboardPage from './modules/dashboard/pages/DashboardPage';
import EndpointListPage from './modules/endpoints/pages/EndpointListPage';
import EndpointDetailPage from './modules/endpoints/pages/EndpointDetailPage';
import LogListPage from './modules/logs/pages/LogListPage';
import ImportExportPage from './modules/import-export/pages/ImportExportPage';
import ProxyConfigPage from './modules/proxy/pages/ProxyConfigPage';
import ScenarioListPage from './modules/scenarios/pages/ScenarioListPage';
import ScenarioDetailPage from './modules/scenarios/pages/ScenarioDetailPage';
import NotFoundPage from './shared/pages/NotFoundPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        const status = (error as { response?: { status?: number } })?.response?.status;
        if (status === 404 || status === 403) return false;
        return failureCount < 1;
      },
    },
  },
});

const fontFamily =
  "-apple-system, BlinkMacSystemFont, 'SF Pro Display', 'SF Pro Text', system-ui, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

function ThemedApp() {
  const { mode } = useTheme();
  const { i18n } = useTranslation();
  const isDark = mode === 'dark';
  const antLocale = i18n.language === 'en' ? enUS : zhTW;

  return (
    <ConfigProvider
      locale={antLocale}
      theme={{
        algorithm: isDark ? theme.darkAlgorithm : theme.defaultAlgorithm,
        token: {
          colorPrimary: isDark ? '#0A84FF' : '#007AFF',
          borderRadius: 12,
          colorBgContainer: isDark ? '#1C1C1E' : '#FFFFFF',
          colorBgLayout: isDark ? '#000000' : '#F5F5F7',
          fontFamily,
          colorBorder: isDark ? '#38383A' : '#E5E5EA',
          colorText: isDark ? '#F5F5F7' : '#1D1D1F',
          colorTextSecondary: isDark ? '#98989D' : '#86868B',
          colorBgElevated: isDark ? '#2C2C2E' : '#FFFFFF',
          colorSuccess: isDark ? '#30D158' : '#34C759',
          colorWarning: isDark ? '#FF9F0A' : '#FF9500',
          colorError: isDark ? '#FF453A' : '#FF3B30',
        },
      }}
    >
      <BrowserRouter>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/endpoints" element={<EndpointListPage />} />
            <Route path="/endpoints/:id" element={<EndpointDetailPage />} />
            <Route path="/logs" element={<LogListPage />} />
            <Route path="/proxy" element={<ProxyConfigPage />} />
            <Route path="/scenarios" element={<ScenarioListPage />} />
            <Route path="/scenarios/:id" element={<ScenarioDetailPage />} />
            <Route path="/import-export" element={<ImportExportPage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ConfigProvider>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <ThemedApp />
      </ThemeProvider>
    </QueryClientProvider>
  );
}
