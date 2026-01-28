import { mockTenantId } from "../setupTest";

export const mockUserId = '00000000-0000-0000-0000-000000000000';

export const userData = {
    "GUID": mockUserId,
    "TenantGUID": mockTenantId,
    "FirstName": "Again Updated",
    "LastName": "User",
    "Email": "anotherbbb@user.com",
    "Password": "password",
    "Active": true,
    "CreatedUtc": "2024-12-27T22:19:02.045989Z",
    "LastUpdateUtc": "2025-01-16T07:14:01.282869Z"
};

export const userMockApiResponse = [
    userData
];