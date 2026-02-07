import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfigProvider } from 'antd';
import AppLayout from './shared/layouts/AppLayout';
import DashboardPage from './modules/dashboard/pages/DashboardPage';
import EndpointListPage from './modules/endpoints/pages/EndpointListPage';
import EndpointDetailPage from './modules/endpoints/pages/EndpointDetailPage';
import LogListPage from './modules/logs/pages/LogListPage';
import ImportExportPage from './modules/import-export/pages/ImportExportPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { refetchOnWindowFocus: false, retry: 1 },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ConfigProvider
        theme={{
          token: { colorPrimary: '#1677ff', borderRadius: 8 },
        }}
      >
        <BrowserRouter>
          <Routes>
            <Route element={<AppLayout />}>
              <Route path="/" element={<DashboardPage />} />
              <Route path="/endpoints" element={<EndpointListPage />} />
              <Route path="/endpoints/:id" element={<EndpointDetailPage />} />
              <Route path="/logs" element={<LogListPage />} />
              <Route path="/import-export" element={<ImportExportPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </ConfigProvider>
    </QueryClientProvider>
  );
}
