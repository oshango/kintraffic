import { StatusPipe } from './status.pipe';

describe('StatusPipe', () => {
  it('create an instance', () => {
    const pipe = new StatusPipe();
    expect(pipe).toBeTruthy();
  });

  it('should map status values to labels', () => {
    const pipe = new StatusPipe();
    expect(pipe.transform('normal')).toBe('Normal');
    expect(pipe.transform('warning')).toBe('Warning');
    expect(pipe.transform('critical')).toBe('Critical');
    expect(pipe.transform('offline')).toBe('Offline');
    expect(pipe.transform(undefined)).toBe('Unknown');
  });
});
