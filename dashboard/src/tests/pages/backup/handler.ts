import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { mockBackupData } from '../mockData';

export const handlers = [
  //update user
  http.get(`${mockEndpoint}v1.0/backups`, () => {
    return HttpResponse.json(mockBackupData);
  }),

  //create backup
  http.post(`${mockEndpoint}v1.0/backups`, () => {
    return HttpResponse.json(mockBackupData);
  }),

  //delete backup
  http.delete(`${mockEndpoint}v1.0/backups/${mockBackupData[0].Filename}`, () => {
    return HttpResponse.json(mockBackupData);
  }),

  //download backup
  http.get(`${mockEndpoint}v1.0/backups/${mockBackupData[0].Filename}`, () => {
    return HttpResponse.json(mockBackupData);
  }),
];
