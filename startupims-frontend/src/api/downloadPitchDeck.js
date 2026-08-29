import api from './axiosInstance';

/**
 * Downloads a startup's pitch deck via an authenticated request and triggers
 * a browser save-as. A plain <a href="..."> can't be used here because the
 * endpoint requires the JWT Authorization header, which only an actual
 * fetch/XHR request (not a simple link navigation) can attach.
 */
export async function downloadPitchDeck(startupId, fileName) {
  const response = await api.get(`/Startups/${startupId}/pitch-deck`, { responseType: 'blob' });
  const url = window.URL.createObjectURL(response.data);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName || 'pitch-deck';
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
