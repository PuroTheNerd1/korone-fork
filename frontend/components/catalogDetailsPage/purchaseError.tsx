export default class PurchaseError extends Error {
  state: string;

  constructor(errorState: string) {
    super('Purchase failed with state ' + errorState);
    this.state = errorState;
  }
}