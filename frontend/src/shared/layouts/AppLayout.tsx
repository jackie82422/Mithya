import { useState } from 'react';
import { Layout, Menu, Typography, Button, Flex, Drawer, Grid, Tooltip } from 'antd';
import {
  DashboardOutlined,
  ApiOutlined,
  FileTextOutlined,
  SwapOutlined,
  CloudServerOutlined,
  BranchesOutlined,
  SunOutlined,
  MoonOutlined,
  MenuOutlined,
  DoubleLeftOutlined,
  DoubleRightOutlined,
} from '@ant-design/icons';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LanguageSwitcher from '../components/LanguageSwitcher';
import { useTheme } from '../contexts/ThemeContext';
import RecordingIndicator from '@/modules/proxy/components/RecordingIndicator';

const { Header, Content } = Layout;
const { useBreakpoint } = Grid;

const SIDER_EXPANDED = 240;
const SIDER_COLLAPSED = 68;

export default function AppLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { mode, toggle } = useTheme();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);

  const menuItems = [
    { key: '/', icon: <DashboardOutlined />, label: t('menu.dashboard') },
    { key: '/endpoints', icon: <ApiOutlined />, label: t('menu.endpoints') },
    { key: '/logs', icon: <FileTextOutlined />, label: t('menu.logs') },
    { key: '/proxy', icon: <CloudServerOutlined />, label: t('menu.proxy') },
    { key: '/scenarios', icon: <BranchesOutlined />, label: t('menu.scenarios') },
    { key: '/import-export', icon: <SwapOutlined />, label: t('menu.importExport') },
  ];

  const selectedKey = menuItems
    .filter((item) => location.pathname.startsWith(item.key) && item.key !== '/')
    .sort((a, b) => b.key.length - a.key.length)[0]?.key || '/';

  const handleMenuClick = ({ key }: { key: string }) => {
    navigate(key);
    if (isMobile) setDrawerOpen(false);
  };

  const siderWidth = isMobile ? 0 : collapsed ? SIDER_COLLAPSED : SIDER_EXPANDED;

  const mobileSidebar = (
    <>
      <div style={{ height: 72, display: 'flex', alignItems: 'center', padding: '0 24px' }}>
        <Typography.Title
          level={4}
          style={{ margin: 0, fontSize: 20, fontWeight: 700, letterSpacing: '-0.3px', color: 'var(--color-text)' }}
        >
          Mithya
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
        <div
          className="apple-sidebar"
          style={{
            position: 'fixed',
            left: 0,
            top: 0,
            bottom: 0,
            width: siderWidth,
            zIndex: 100,
            display: 'flex',
            flexDirection: 'column',
            transition: 'width 0.25s cubic-bezier(0.4, 0, 0.2, 1)',
            overflow: 'hidden',
          }}
        >
          {/* Logo */}
          {collapsed ? (
            <div style={{ height: 56, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
              <img src="/mithya.svg" alt="Mithya" style={{ width: 36, height: 36, borderRadius: 10, border: '1px solid var(--color-border)' }} />
            </div>
          ) : (
            <div style={{ height: 56, display: 'flex', alignItems: 'center', padding: '0 20px', flexShrink: 0 }}>
              <img src="/mithya.svg" alt="Mithya" style={{ width: 28, height: 28, borderRadius: 6, marginRight: 10, flexShrink: 0, border: '1px solid var(--color-border)' }} />
              <Typography.Title
                level={4}
                style={{ margin: 0, fontSize: 18, fontWeight: 700, letterSpacing: '-0.3px', color: 'var(--color-text)', whiteSpace: 'nowrap' }}
              >
                Mithya
              </Typography.Title>
            </div>
          )}

          {/* Menu */}
          <div style={{ flex: 1, overflowX: 'hidden', overflowY: 'auto' }}>
            {collapsed ? (
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4, paddingTop: 4 }}>
                {menuItems.map((item) => (
                  <Tooltip key={item.key} title={item.label} placement="right">
                    <div
                      onClick={() => handleMenuClick({ key: item.key })}
                      style={{
                        width: 40,
                        height: 40,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        borderRadius: 10,
                        cursor: 'pointer',
                        fontSize: 16,
                        color: selectedKey === item.key ? '#fff' : 'var(--color-text-secondary)',
                        background: selectedKey === item.key ? 'var(--color-primary)' : 'transparent',
                        transition: 'all 0.2s ease',
                      }}
                    >
                      {item.icon}
                    </div>
                  </Tooltip>
                ))}
              </div>
            ) : (
              <Menu
                theme="light"
                mode="inline"
                selectedKeys={[selectedKey]}
                items={menuItems}
                onClick={handleMenuClick}
                style={{ border: 'none', padding: '0 4px' }}
              />
            )}
          </div>

          {/* Collapse toggle */}
          <div style={{ padding: 8, borderTop: '1px solid var(--sidebar-border)', flexShrink: 0, display: 'flex', justifyContent: collapsed ? 'center' : 'flex-end' }}>
            <Button
              type="text"
              icon={collapsed ? <DoubleRightOutlined /> : <DoubleLeftOutlined />}
              onClick={() => setCollapsed((v) => !v)}
              style={{ color: 'var(--color-text-secondary)' }}
            />
          </div>
        </div>
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
          {mobileSidebar}
        </Drawer>
      )}

      <Layout style={{ marginLeft: siderWidth, transition: 'margin-left 0.25s cubic-bezier(0.4, 0, 0.2, 1)' }}>
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
          <Flex align="center" gap={8}>
            <RecordingIndicator />
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
