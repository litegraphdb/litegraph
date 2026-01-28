import * as sdk from '../src';
// var sdk = require('../dist');
export const mockTenantId = 'default';
export const mockEndpoint = 'http://localhost:8701/v1.0/mock/'; //endpoint
export const mockAccessToken = 'default'; //accessToken

export const api = new sdk.LiteGraphSdk(
    mockEndpoint, //endpoint
    mockTenantId,
    mockAccessToken
);

export { sdk };