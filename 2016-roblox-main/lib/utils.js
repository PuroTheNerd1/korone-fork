export const isTouchDevice = () => {
  // From https://stackoverflow.com/questions/4817029/whats-the-best-way-to-detect-a-touch-screen-device-using-javascript
  return (('ontouchstart' in window) ||
    (navigator.maxTouchPoints > 0) ||
    // @ts-ignore
    (navigator.msMaxTouchPoints > 0));
}

export const Random = (min, max) => {
  return Math.floor(Math.random() * (max - min) ) + min;
}

/**
 * @param {number} seconds
 * @returns {Promise}
 */
export const wait = (seconds) =>
    new Promise(resolve => setTimeout(resolve, seconds * 1000));

export function IsNullOrEmpty(value) {
  return !value || value.trim().length === 0;
}