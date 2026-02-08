import { useState } from 'react';
import { Layout, Menu, Typography, Button, Flex, Drawer, Grid } from 'antd';
import {
  DashboardOutlined,
  ApiOutlined,
  FileTextOutlined,
  SwapOutlined,
  SunOutlined,
  MoonOutlined,
  MenuOutlined,
} from '@ant-design/icons';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LanguageSwitcher from '../components/LanguageSwitcher';
import { useTheme } from '../contexts/ThemeContext';

const { Sider, Header, Content } = Layout;
const { useBreakpoint } = Grid;

export default function AppLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { mode, toggle } = useTheme();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const [drawerOpen, setDrawerOpen] = useState(false);

  const menuItems = [
    { key: '/', icon: <DashboardOutlined />, label: t('menu.dashboard') },
    { key: '/endpoints', icon: <ApiOutlined />, label: t('menu.endpoints') },
    { key: '/logs', icon: <FileTextOutlined />, label: t('menu.logs') },
    { key: '/import-export', icon: <SwapOutlined />, label: t('menu.importExport') },
  ];

  const selectedKey = menuItems
    .filter((item) => location.pathname.startsWith(item.key) && item.key !== '/')
    .sort((a, b) => b.key.length - a.key.length)[0]?.key || '/';

  const handleMenuClick = ({ key }: { key: string }) => {
    navigate(key);
    if (isMobile) setDrawerOpen(false);
  };

  const sidebarContent = (
    <>
      <div
        style={{
          height: 72,
          display: 'flex',
          alignItems: 'center',
          padding: '0 24px',
        }}
      >
        <Typography.Title
          level={4}
          style={{
            margin: 0,
            fontSize: 20,
            fontWeight: 700,
            letterSpacing: '-0.3px',
            color: 'var(--color-text)',
          }}
        >
          Mock Server
        </Typography.Title>
      </div>
      <Menu
        theme="light"
        mode="inline"
        selectedKeys={[selectedKey]}
        items={menuItems}
        onClick={handleMenuClick}
        style={{ border: 'none', padding: '0 4px' }}
      />
    </>
  );

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {!isMobile && (
        <Sider
          width={240}
          theme="light"
          className="apple-sidebar"
          style={{
            position: 'fixed',
            left: 0,
            top: 0,
            bottom: 0,
            zIndex: 100,
            overflow: 'auto',
          }}
        >
          {sidebarContent}
        </Sider>
      )}

      {isMobile && (
        <Drawer
          placement="left"
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          width={260}
          styles={{ body: { padding: 0 } }}
          closable={false}
        >
          {sidebarContent}
        </Drawer>
      )}

      <Layout style={{ marginLeft: isMobile ? 0 : 240 }}>
        <Header
          className="apple-header"
          style={{
            padding: isMobile ? '0 16px' : '0 32px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            height: 56,
            lineHeight: '56px',
            position: 'sticky',
            top: 0,
            zIndex: 99,
          }}
        >
          <div>
            {isMobile && (
              <Button
                type="text"
                icon={<MenuOutlined />}
                onClick={() => setDrawerOpen(true)}
                style={{ color: 'var(--color-text)' }}
              />
            )}
          </div>
          <Flex align="center" gap={4}>
            <Button
              type="text"
              icon={mode === 'dark' ? <SunOutlined /> : <MoonOutlined />}
              onClick={toggle}
              style={{ color: 'var(--color-text-secondary)' }}
            />
            <LanguageSwitcher />
          </Flex>
        </Header>
        <Content
          style={{
            padding: isMobile ? 16 : 32,
            maxWidth: 1200,
            width: '100%',
            margin: '0 auto',
          }}
        >
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
