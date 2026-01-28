/**
 * Enum for API error codes.
 *
 * @readonly
 * @enum {string}
 */
export const ApiErrorEnum = Object.freeze({
  /** No object metadata supplied. */
  NoObjectMetadata: 'NoObjectMetadata',

  /** No object data supplied. */
  NoObjectData: 'NoObjectData',

  /** No metadata rule supplied. */
  NoMetadataRule: 'NoMetadataRule',

  /** Required properties were missing. */
  RequiredPropertiesMissing: 'RequiredPropertiesMissing',

  /** No connectivity to the specified graph. */
  NoGraphConnectivity: 'NoGraphConnectivity',

  /** A graph operation has failed. */
  GraphOperationFailed: 'GraphOperationFailed',

  /** No connectivity to the type detection endpoint. */
  NoTypeDetectorConnectivity: 'NoTypeDetectorConnectivity',

  /** Unable to discern type of supplied data. */
  UnknownTypeDetected: 'UnknownTypeDetected',

  /** No connectivity to the UDR endpoint. */
  NoUdrConnectivity: 'NoUdrConnectivity',

  /** UDR generation failed. */
  UdrGenerationFailed: 'UdrGenerationFailed',

  /** No connectivity to the semantic cell extraction endpoint. */
  NoSemanticCellConnectivity: 'NoSemanticCellConnectivity',

  /** Semantic cell extraction failed. */
  SemanticCellExtractionFailed: 'SemanticCellExtractionFailed',

  /** No data catalog connectivity. */
  NoDataCatalogConnectivity: 'NoDataCatalogConnectivity',

  /** Persisting data in the data catalog failed. */
  DataCatalogPersistFailed: 'DataCatalogPersistFailed',

  /** Unknown data catalog type. */
  UnknownDataCatalogType: 'UnknownDataCatalogType',

  /** Unknown embeddings generator type. */
  UnknownEmbeddingsGeneratorType: 'UnknownEmbeddingsGeneratorType',

  /** Embeddings persistence failed. */
  EmbeddingsPersistFailed: 'EmbeddingsPersistFailed',

  /** Embeddings generation failed. */
  EmbeddingsGenerationFailed: 'EmbeddingsGenerationFailed',

  /** Authentication failed. */
  AuthenticationFailed: 'AuthenticationFailed',

  /** Authorization failed. */
  AuthorizationFailed: 'AuthorizationFailed',

  /** Bad request. */
  BadRequest: 'BadRequest',

  /** Conflict. */
  Conflict: 'Conflict',

  /** DeserializationError. */
  DeserializationError: 'DeserializationError',

  /** Inactive. */
  Inactive: 'Inactive',

  /** Internal error. */
  InternalError: 'InternalError',

  /** Invalid range. */
  InvalidRange: 'InvalidRange',

  /** In use. */
  InUse: 'InUse',

  /** Not empty. */
  NotEmpty: 'NotEmpty',

  /** Not found. */
  NotFound: 'NotFound',

  /** Request too large. */
  TooLarge: 'TooLarge',
});
