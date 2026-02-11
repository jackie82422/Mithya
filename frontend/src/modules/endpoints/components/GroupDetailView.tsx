import { useState, useMemo, useCallback } from 'react';
import { Typography, Button, Flex, Breadcrumb, Empty, Input, Checkbox, Modal } from 'antd';
import { ArrowLeftOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { MockEndpoint, EndpointGroup, SetDefaultResponseRequest } from '@/shared/types';
import { useEndpoints, useDeleteEndpoint, useToggleEndpoint, useSetDefaultResponse } from '../hooks';
import { useGroup, useAddEndpointsToGroup, useRemoveEndpointFromGroup } from '../groupHooks';
import EndpointCard from './EndpointCard';
import EndpointForm from './EndpointForm';
import DefaultResponseForm from './DefaultResponseForm';
import type { CreateEndpointRequest, UpdateEndpointRequest } from '@/shared/types';
import { useCreateEndpoint, useUpdateEndpoint } from '../hooks';

interface GroupDetailViewProps {
  groupId: string;
  isUngrouped?: boolean;
  ungroupedEndpoints?: MockEndpoint[];
  onBack: () => void;
}

export default function GroupDetailView({ groupId, isUngrouped, ungroupedEndpoints, onBack }: GroupDetailViewProps) {
  const { t } = useTranslation();
  const { data: group } = useGroup(groupId);
  const { data: allEndpoints } = useEndpoints();
  const deleteEndpoint = useDeleteEndpoint();
  const toggleEndpoint = useToggleEndpoint();
  const setDefault = useSetDefaultResponse();
  const createEndpoint = useCreateEndpoint();
  const updateEndpoint = useUpdateEndpoint();
  const addEndpoints = useAddEndpointsToGroup();
  const removeEndpoint = useRemoveEndpointFromGroup();

  const [formOpen, setFormOpen] = useState(false);
  const [editingEndpoint, setEditingEndpoint] = useState<MockEndpoint | null>(null);
  const [defaultFormOpen, setDefaultFormOpen] = useState(false);
  const [selectedEndpoint, setSelectedEndpoint] = useState<MockEndpoint | null>(null);
  const [addModalOpen, setAddModalOpen] = useState(false);
  const [addSearch, setAddSearch] = useState('');
  const [addSelected, setAddSelected] = useState<Set<string>>(new Set());

  const endpoints = isUngrouped ? ungroupedEndpoints : group?.endpoints;
  const groupName = isUngrouped ? t('groups.ungrouped') : group?.name ?? '';
  const groupColor = isUngrouped ? undefined : group?.color || '#1677ff';

  const existingIds = useMemo(() => new Set((endpoints ?? []).map((ep) => ep.id)), [endpoints]);

  const availableEndpoints = useMemo(() => {
    if (!allEndpoints) return [];
    let list = allEndpoints.filter((ep) => !existingIds.has(ep.id));
    if (addSearch) {
      const q = addSearch.toLowerCase();
      list = list.filter(
        (ep) =>
          ep.name.toLowerCase().includes(q) ||
          ep.path.toLowerCase().includes(q) ||
          ep.serviceName.toLowerCase().includes(q),
      );
    }
    return list;
  }, [allEndpoints, existingIds, addSearch]);

  const handleAddEndpoints = () => {
    if (addSelected.size === 0) return;
    addEndpoints.mutate(
      { groupId, endpointIds: Array.from(addSelected) },
      {
        onSuccess: () => {
          setAddModalOpen(false);
          setAddSelected(new Set());
          setAddSearch('');
        },
      },
    );
  };

  const toggleAddSelect = useCallback((id: string) => {
    setAddSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

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

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <a onClick={onBack}>{t('endpoints.title')}</a> },
          { title: groupName },
        ]}
      />

      <Flex justify="space-between" align="center" style={{ marginBottom: 20 }}>
        <Flex align="center" gap={12}>
          <Button type="text" icon={<ArrowLeftOutlined />} onClick={onBack} />
          {groupColor && (
            <span style={{ width: 14, height: 14, borderRadius: '50%', background: groupColor, flexShrink: 0 }} />
          )}
          <Typography.Title level={3} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
            {groupName}
          </Typography.Title>
        </Flex>
        {!isUngrouped && (
          <Button icon={<PlusOutlined />} onClick={() => { setAddModalOpen(true); setAddSelected(new Set()); setAddSearch(''); }}>
            {t('groups.addEndpoints')}
          </Button>
        )}
      </Flex>

      {!endpoints?.length ? (
        <Empty description={t('common.noData')} />
      ) : (
        endpoints.map((ep) => (
          <EndpointCard
            key={ep.id}
            endpoint={ep}
            onDelete={(id) => deleteEndpoint.mutate(id)}
            onSetDefault={(ep) => { setSelectedEndpoint(ep); setDefaultFormOpen(true); }}
            onToggle={(id) => toggleEndpoint.mutate(id)}
            onEdit={(ep) => { setEditingEndpoint(ep); setFormOpen(true); }}
            toggleLoading={toggleEndpoint.isPending}
            inGroupView={!isUngrouped}
            onRemoveFromGroup={
              !isUngrouped
                ? (endpointId) => removeEndpoint.mutate({ groupId, endpointId })
                : undefined
            }
          />
        ))
      )}

      {/* Add endpoints to group modal */}
      <Modal
        title={t('groups.addEndpoints')}
        open={addModalOpen}
        onCancel={() => setAddModalOpen(false)}
        onOk={handleAddEndpoints}
        confirmLoading={addEndpoints.isPending}
        okText={t('common.save')}
        cancelText={t('common.cancel')}
        okButtonProps={{ disabled: addSelected.size === 0 }}
        width={600}
        destroyOnClose
      >
        <Input
          prefix={<SearchOutlined />}
          placeholder={t('common.search')}
          value={addSearch}
          onChange={(e) => setAddSearch(e.target.value)}
          style={{ marginBottom: 12 }}
          allowClear
        />
        <div style={{ maxHeight: 400, overflowY: 'auto' }}>
          {availableEndpoints.length === 0 ? (
            <Empty description={t('common.noData')} />
          ) : (
            availableEndpoints.map((ep) => (
              <Flex
                key={ep.id}
                align="center"
                gap={10}
                style={{
                  padding: '8px 4px',
                  borderBottom: '1px solid var(--color-border)',
                  cursor: 'pointer',
                }}
                onClick={() => toggleAddSelect(ep.id)}
              >
                <Checkbox checked={addSelected.has(ep.id)} />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <Typography.Text strong style={{ fontSize: 13 }}>{ep.name}</Typography.Text>
                  <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
                    {ep.httpMethod} {ep.path} — {ep.serviceName}
                  </Typography.Text>
                </div>
              </Flex>
            ))
          )}
        </div>
      </Modal>

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
    </div>
  );
}
