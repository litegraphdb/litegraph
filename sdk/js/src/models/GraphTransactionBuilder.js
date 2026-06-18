import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';
import TransactionOperation from './TransactionOperation';

export default class GraphTransactionBuilder {
  constructor(sdk, graphGuid, options = {}) {
    this._sdk = sdk;
    this.graphGuid = graphGuid;
    this.maxOperations = options.MaxOperations || options.maxOperations || 1000;
    this.timeoutSeconds = options.TimeoutSeconds || options.timeoutSeconds || 60;
    this.isolationLevel = options.IsolationLevel || options.isolationLevel || 'Default';
    this.operations = [];
  }

  withMaxOperations(maxOperations) {
    if (maxOperations < 1 || maxOperations > 10000) {
      GenericExceptionHandlers.GenericException('MaxOperations must be between 1 and 10000.');
    }
    this.maxOperations = maxOperations;
    return this;
  }

  withTimeoutSeconds(timeoutSeconds) {
    if (timeoutSeconds < 1 || timeoutSeconds > 3600) {
      GenericExceptionHandlers.GenericException('TimeoutSeconds must be between 1 and 3600.');
    }
    this.timeoutSeconds = timeoutSeconds;
    return this;
  }

  withIsolationLevel(isolationLevel) {
    const allowed = ['Default', 'ReadCommitted', 'RepeatableRead', 'Serializable'];
    if (!allowed.includes(isolationLevel)) {
      GenericExceptionHandlers.GenericException('IsolationLevel must be Default, ReadCommitted, RepeatableRead, or Serializable.');
    }
    this.isolationLevel = isolationLevel;
    return this;
  }

  add(operation) {
    if (!operation) {
      GenericExceptionHandlers.ArgumentNullException('Operation');
    }
    this.operations.push(new TransactionOperation(operation));
    return this;
  }

  create(objectType, payload) {
    return this._payloadOperation('Create', objectType, payload);
  }

  update(objectType, payload, guid = null) {
    return this._payloadOperation('Update', objectType, payload, guid);
  }

  delete(objectType, guid) {
    return this.add({ OperationType: 'Delete', ObjectType: objectType, GUID: guid });
  }

  attach(objectType, payload, guid = null) {
    return this._payloadOperation('Attach', objectType, payload, guid);
  }

  detach(objectType, guid) {
    return this.add({ OperationType: 'Detach', ObjectType: objectType, GUID: guid });
  }

  upsert(objectType, payload, guid = null) {
    return this._payloadOperation('Upsert', objectType, payload, guid);
  }

  createNode(payload) {
    return this.create('Node', payload);
  }

  updateNode(payload, guid = null) {
    return this.update('Node', payload, guid);
  }

  deleteNode(guid) {
    return this.delete('Node', guid);
  }

  upsertNode(payload, guid = null) {
    return this.upsert('Node', payload, guid);
  }

  createEdge(payload) {
    return this.create('Edge', payload);
  }

  updateEdge(payload, guid = null) {
    return this.update('Edge', payload, guid);
  }

  deleteEdge(guid) {
    return this.delete('Edge', guid);
  }

  upsertEdge(payload, guid = null) {
    return this.upsert('Edge', payload, guid);
  }

  createLabel(payload) {
    return this.create('Label', payload);
  }

  updateLabel(payload, guid = null) {
    return this.update('Label', payload, guid);
  }

  deleteLabel(guid) {
    return this.delete('Label', guid);
  }

  attachLabel(payload, guid = null) {
    return this.attach('Label', payload, guid);
  }

  detachLabel(guid) {
    return this.detach('Label', guid);
  }

  upsertLabel(payload, guid = null) {
    return this.upsert('Label', payload, guid);
  }

  createTag(payload) {
    return this.create('Tag', payload);
  }

  updateTag(payload, guid = null) {
    return this.update('Tag', payload, guid);
  }

  deleteTag(guid) {
    return this.delete('Tag', guid);
  }

  attachTag(payload, guid = null) {
    return this.attach('Tag', payload, guid);
  }

  detachTag(guid) {
    return this.detach('Tag', guid);
  }

  upsertTag(payload, guid = null) {
    return this.upsert('Tag', payload, guid);
  }

  createVector(payload) {
    return this.create('Vector', payload);
  }

  updateVector(payload, guid = null) {
    return this.update('Vector', payload, guid);
  }

  deleteVector(guid) {
    return this.delete('Vector', guid);
  }

  attachVector(payload, guid = null) {
    return this.attach('Vector', payload, guid);
  }

  detachVector(guid) {
    return this.detach('Vector', guid);
  }

  upsertVector(payload, guid = null) {
    return this.upsert('Vector', payload, guid);
  }

  build() {
    return {
      MaxOperations: this.maxOperations,
      TimeoutSeconds: this.timeoutSeconds,
      IsolationLevel: this.isolationLevel,
      Operations: this.operations.map((operation) => ({ ...operation })),
    };
  }

  async execute(cancellationToken) {
    return await this._sdk.executeTransaction(this.graphGuid, this.build(), cancellationToken);
  }

  _payloadOperation(operationType, objectType, payload, guid = null) {
    if (!payload) {
      GenericExceptionHandlers.ArgumentNullException('Payload');
    }
    return this.add({
      OperationType: operationType,
      ObjectType: objectType,
      GUID: guid,
      Payload: payload,
    });
  }
}
