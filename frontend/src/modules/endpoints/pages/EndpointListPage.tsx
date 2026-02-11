import { useState, useRef, useEffect, useCallback, useMemo } from 'react';
import { Typography, Button, Spin, Empty, Input, Flex, Space, Popconfirm, message, Segmented, Card } from 'antd';
import type { InputRef } from 'antd';
import { PlusOutlined, SearchOutlined, CheckSquareOutlined, CloseOutlined, GroupOutlined, UnorderedListOutlined, AppstoreOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useEndpoints, useCreateEndpoint, useUpdateEndpoint, useDeleteEndpoint, useSetDefaultResponse, useToggleEndpoint } from '../hooks';
import { useGroupsWithEndpoints, useDeleteGroup } from '../groupHooks';
import EndpointCard from '../components/EndpointCard';
import EndpointForm from '../components/EndpointForm';
import DefaultResponseForm from '../components/DefaultResponseForm';
import GroupCard from '../components/GroupCard';
import GroupDetailView from '../components/GroupDetailView';
import GroupManageModal from '../components/GroupManageModal';
import GroupAssignModal from '../components/GroupAssignModal';
import type { MockEndpoint, EndpointGroup, CreateEndpointRequest, UpdateEndpointRequest, SetDefaultResponseRequest } from '@/shared/types';

type ViewMode = 'list' | 'groups';

export default function EndpointListPage() {
  const { t } = useTranslation();
  const { data: endpoints, isLoading } = useEndpoints();
  const createEndpoint = useCreateEndpoint();
  const updateEndpoint = useUpdateEndpoint();
  const deleteEndpoint = useDeleteEndpoint();
  const setDefault = useSetDefaultResponse();
  const toggleEndpoint = useToggleEndpoint();
  const deleteGroup = useDeleteGroup();

  const [formOpen, setFormOpen] = useState(false);
  const [editingEndpoint, setEditingEndpoint] = useState<MockEndpoint | null>(null);
  const [defaultFormOpen, setDefaultFormOpen] = useState(false);
  const [selectedEndpoint, setSelectedEndpoint] = useState<MockEndpoint | null>(null);
  const [search, setSearch] = useState('');
  const searchRef = useRef<InputRef>(null);

  const [batchMode, setBatchMode] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [batchLoading, setBatchLoading] = useState(false);

  const { groups: allGroups, endpointGroupMap, groupedEndpointIds } = useGroupsWithEndpoints();

  const [viewMode, setViewMode] = useState<ViewMode>('groups');
  const [activeGroupId, setActiveGroupId] = useState<string | null>(null);
  const [groupManageOpen, setGroupManageOpen] = useState(false);
  const [groupAssignOpen, setGroupAssignOpen] = useState(false);
  const [editingGroup, setEditingGroup] = useState<EndpointGroup | null>(null);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        searchRef.current?.focus();
      }
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, []);

  // List mode: text search filtered
  const filtered = useMemo(() => {
    let list = endpoints ?? [];
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(
        (ep) =>
          ep.name.toLowerCase().includes(q) ||
          ep.path.toLowerCase().includes(q) ||
          ep.serviceName.toLowerCase().includes(q) ||
          ep.httpMethod.toLowerCase().includes(q),
      );
    }
    return list;
  }, [endpoints, search]);

  // Ungrouped endpoints for group mode
  const ungroupedEndpoints = useMemo(
    () => (endpoints ?? []).filter((ep) => !groupedEndpointIds.has(ep.id)),
    [endpoints, groupedEndpointIds],
  );

  const handleCreate = (values: CreateEndpointRequest) => {
    if (editingEndpoint) {
      const updateData: UpdateEndpointRequest = {
        name: values.name,
        serviceName: values.serviceName,
        path: values.path,
        httpMethod: values.httpMethod,
        protocolSettings: values.protocolSettings,
      };
      updateEndpoint.mutate(
        { id: editingEndpoint.id, data: updateData },
        { onSuccess: () => { setFormOpen(false); setEditingEndpoint(null); } },
      );
    } else {
      createEndpoint.mutate(values, { onSuccess: () => setFormOpen(false) });
    }
  };

  const handleSetDefault = (id: string, data: SetDefaultResponseRequest) => {
    setDefault.mutate({ id, data }, { onSuccess: () => setDefaultFormOpen(false) });
  };

  const handleSelectToggle = useCallback((id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

  const exitBatchMode = () => {
    setBatchMode(false);
    setSelectedIds(new Set());
  };

  const runBatch = async (action: (id: string) => Promise<unknown>) => {
    setBatchLoading(true);
    const ids = Array.from(selectedIds);
    let successCount = 0;
    for (const id of ids) {
      try {
        await action(id);
        successCount++;
      } catch {
        // individual errors handled by mutation
      }
    }
    setBatchLoading(false);
    if (successCount > 0) {
      message.success(t('endpoints.batch.done', { count: successCount }));
    }
    exitBatchMode();
  };

  const handleBatchEnable = () => {
    const toEnable = Array.from(selectedIds).filter((id) => {
      const ep = endpoints?.find((e) => e.id === id);
      return ep && !ep.isActive;
    });
    if (toEnable.length === 0) {
      message.info(t('endpoints.batch.noneToEnable'));
      return;
    }
    setSelectedIds(new Set(toEnable));
    runBatch((id) => toggleEndpoint.mutateAsync(id));
  };

  const handleBatchDisable = () => {
    const toDisable = Array.from(selectedIds).filter((id) => {
      const ep = endpoints?.find((e) => e.id === id);
      return ep && ep.isActive;
    });
    if (toDisable.length === 0) {
      message.info(t('endpoints.batch.noneToDisable'));
      return;
    }
    setSelectedIds(new Set(toDisable));
    runBatch((id) => toggleEndpoint.mutateAsync(id));
  };

  const handleBatchDelete = () => {
    runBatch((id) => deleteEndpoint.mutateAsync(id));
  };

  const selectedCount = selectedIds.size;

  // If a group is active in group mode, show GroupDetailView
  if (viewMode === 'groups' && activeGroupId) {
    return (
      <GroupDetailView
        groupId={activeGroupId}
        isUngrouped={activeGroupId === 'ungrouped'}
        ungroupedEndpoints={activeGroupId === 'ungrouped' ? ungroupedEndpoints : undefined}
        onBack={() => setActiveGroupId(null)}
      />
    );
  }

  return (
    <div>
      <Flex justify="space-between" align="center" style={{ marginBottom: 24 }}>
        <div>
          <Typography.Title level={3} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
            {t('endpoints.title')}
          </Typography.Title>
          <Typography.Text type="secondary" style={{ fontSize: 14 }}>
            {t('endpoints.subtitle')}
          </Typography.Text>
        </div>
        <Space>
          <Segmented
            value={viewMode}
            onChange={(v) => { setViewMode(v as ViewMode); exitBatchMode(); }}
            options={[
              { value: 'list', icon: <UnorderedListOutlined /> },
              { value: 'groups', icon: <AppstoreOutlined /> },
            ]}
          />
          {viewMode === 'list' && !batchMode && filtered && filtered.length > 0 && (
            <Button
              icon={<CheckSquareOutlined />}
              onClick={() => setBatchMode(true)}
            >
              {t('endpoints.batch.toggle')}
            </Button>
          )}
          {batchMode && (
            <Button icon={<CloseOutlined />} onClick={exitBatchMode}>
              {t('common.cancel')}
            </Button>
          )}
          <Button type="primary" icon={<PlusOutlined />} onClick={() => { setEditingEndpoint(null); setFormOpen(true); }}>
            {t('endpoints.create')}
          </Button>
        </Space>
      </Flex>

      {/* ── List Mode ── */}
      {viewMode === 'list' && (
        <>
          <Input
            ref={searchRef}
            prefix={<SearchOutlined style={{ color: 'var(--color-text-secondary)' }} />}
            placeholder={`${t('common.search')}  ⌘K`}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ marginBottom: 20, height: 42, borderRadius: 12 }}
            allowClear
          />

          {isLoading ? (
            <Flex justify="center" style={{ padding: 80 }}>
              <Spin size="large" />
            </Flex>
          ) : !filtered?.length ? (
            <Empty description={search ? t('common.noData') : t('endpoints.noEndpoints')}>
              {!search && (
                <Button type="primary" onClick={() => setFormOpen(true)}>
                  {t('endpoints.create')}
                </Button>
              )}
            </Empty>
          ) : (
            filtered.map((ep) => (
              <EndpointCard
                key={ep.id}
                endpoint={ep}
                onDelete={(id) => deleteEndpoint.mutate(id)}
                onSetDefault={(ep) => {
                  setSelectedEndpoint(ep);
                  setDefaultFormOpen(true);
                }}
                onToggle={(id) => toggleEndpoint.mutate(id)}
                onEdit={(ep) => {
                  setEditingEndpoint(ep);
                  setFormOpen(true);
                }}
                toggleLoading={toggleEndpoint.isPending}
                selectable={batchMode}
                selected={selectedIds.has(ep.id)}
                onSelect={handleSelectToggle}
                groups={endpointGroupMap[ep.id]}
                onGroupClick={(groupId) => {
                  setViewMode('groups');
                  setActiveGroupId(groupId);
                }}
              />
            ))
          )}
        </>
      )}

      {/* ── Group Mode ── */}
      {viewMode === 'groups' && (
        <>
          {isLoading ? (
            <Flex justify="center" style={{ padding: 80 }}>
              <Spin size="large" />
            </Flex>
          ) : (
            <>
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
                  gap: 16,
                  marginBottom: 16,
                }}
              >
                {allGroups.map((g) => (
                  <GroupCard
                    key={g.id}
                    group={g}
                    onOpen={() => setActiveGroupId(g.id)}
                    onEdit={(group) => { setEditingGroup(group); setGroupManageOpen(true); }}
                    onDelete={(id) => deleteGroup.mutate(id)}
                  />
                ))}

                {/* Ungrouped card */}
                <Card
                  hoverable
                  style={{
                    borderRadius: 16,
                    cursor: 'pointer',
                    border: '1px dashed var(--color-border)',
                    height: '100%',
                  }}
                  styles={{ body: { padding: '16px 20px' } }}
                  onClick={() => setActiveGroupId('ungrouped')}
                >
                  <Typography.Text strong style={{ fontSize: 15, display: 'block', marginBottom: 4 }}>
                    {t('groups.ungrouped')}
                  </Typography.Text>
                  <Typography.Text type="secondary" style={{ fontSize: 13 }}>
                    {t('groups.endpointCount', { count: ungroupedEndpoints.length })}
                  </Typography.Text>
                </Card>
              </div>

              <Button
                type="dashed"
                icon={<PlusOutlined />}
                onClick={() => { setEditingGroup(null); setGroupManageOpen(true); }}
                block
                style={{ borderRadius: 12, height: 44 }}
              >
                {t('groups.create')}
              </Button>
            </>
          )}
        </>
      )}

      {/* ── Batch Action Bar ── */}
      {batchMode && selectedCount > 0 && (
        <div
          style={{
            position: 'fixed',
            bottom: 24,
            left: '50%',
            transform: 'translateX(-50%)',
            background: 'var(--color-surface)',
            border: '1px solid var(--color-border)',
            borderRadius: 16,
            padding: '12px 24px',
            boxShadow: '0 8px 32px rgba(0,0,0,0.12)',
            zIndex: 100,
            backdropFilter: 'blur(12px)',
          }}
        >
          <Flex align="center" gap={16}>
            <Typography.Text strong>
              {t('endpoints.batch.selected', { count: selectedCount })}
            </Typography.Text>
            <Button size="small" icon={<GroupOutlined />} onClick={() => setGroupAssignOpen(true)}>
              {t('groups.addToGroup')}
            </Button>
            <Button size="small" onClick={handleBatchEnable} loading={batchLoading}>
              {t('endpoints.batch.enable')}
            </Button>
            <Button size="small" onClick={handleBatchDisable} loading={batchLoading}>
              {t('endpoints.batch.disable')}
            </Button>
            <Popconfirm
              title={t('endpoints.batch.deleteConfirm', { count: selectedCount })}
              onConfirm={handleBatchDelete}
              okText={t('common.yes')}
              cancelText={t('common.no')}
            >
              <Button size="small" danger loading={batchLoading}>
                {t('common.delete')}
              </Button>
            </Popconfirm>
          </Flex>
        </div>
      )}

      <EndpointForm
        open={formOpen}
        onCancel={() => { setFormOpen(false); setEditingEndpoint(null); }}
        onSubmit={handleCreate}
        loading={editingEndpoint ? updateEndpoint.isPending : createEndpoint.isPending}
        editingEndpoint={editingEndpoint}
      />

      <DefaultResponseForm
        open={defaultFormOpen}
        endpoint={selectedEndpoint}
        onCancel={() => setDefaultFormOpen(false)}
        onSubmit={handleSetDefault}
        loading={setDefault.isPending}
      />

      <GroupManageModal
        open={groupManageOpen}
        onClose={() => { setGroupManageOpen(false); setEditingGroup(null); }}
        groups={allGroups}
        editingGroup={editingGroup}
      />

      <GroupAssignModal
        open={groupAssignOpen}
        onClose={() => { setGroupAssignOpen(false); exitBatchMode(); }}
        groups={allGroups}
        endpointIds={Array.from(selectedIds)}
      />
    </div>
  );
}
