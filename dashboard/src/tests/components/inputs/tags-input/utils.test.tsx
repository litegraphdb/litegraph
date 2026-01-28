import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';

describe('TagsInput Utils', () => {
  describe('convertTagsToRecord', () => {
    it('converts empty array to empty record', () => {
      const result = convertTagsToRecord([]);
      expect(result).toEqual({});
    });

    it('converts single tag to record', () => {
      const tags = [{ key: 'name', value: 'John Doe' }];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({ name: 'John Doe' });
    });

    it('converts multiple tags to record', () => {
      const tags = [
        { key: 'name', value: 'John Doe' },
        { key: 'age', value: '30' },
        { key: 'city', value: 'New York' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        name: 'John Doe',
        age: '30',
        city: 'New York',
      });
    });

    it('handles tags with empty strings', () => {
      const tags = [
        { key: 'name', value: '' },
        { key: 'description', value: 'Some description' },
        { key: 'empty', value: '' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        name: '',
        description: 'Some description',
        empty: '',
      });
    });

    it('handles tags with whitespace-only values', () => {
      const tags = [
        { key: 'name', value: '   ' },
        { key: 'description', value: '\t\n' },
        { key: 'normal', value: 'normal value' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        name: '   ',
        description: '\t\n',
        normal: 'normal value',
      });
    });

    it('handles tags with special characters', () => {
      const tags = [
        { key: 'email', value: 'user@example.com' },
        { key: 'phone', value: '+1-555-123-4567' },
        { key: 'symbols', value: '!@#$%^&*()' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        email: 'user@example.com',
        phone: '+1-555-123-4567',
        symbols: '!@#$%^&*()',
      });
    });

    it('handles tags with unicode characters', () => {
      const tags = [
        { key: 'emoji', value: '🚀🌟🎉' },
        { key: 'accent', value: 'café résumé naïve' },
        { key: 'chinese', value: '你好世界' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        emoji: '🚀🌟🎉',
        accent: 'café résumé naïve',
        chinese: '你好世界',
      });
    });

    it('handles tags with numbers as strings', () => {
      const tags = [
        { key: 'count', value: '42' },
        { key: 'price', value: '99.99' },
        { key: 'year', value: '2023' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        count: '42',
        price: '99.99',
        year: '2023',
      });
    });

    it('handles tags with boolean-like strings', () => {
      const tags = [
        { key: 'active', value: 'true' },
        { key: 'verified', value: 'false' },
        { key: 'enabled', value: 'yes' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        active: 'true',
        verified: 'false',
        enabled: 'yes',
      });
    });

    it('handles tags with JSON-like strings', () => {
      const tags = [
        { key: 'config', value: '{"theme": "dark", "lang": "en"}' },
        { key: 'metadata', value: '{"version": "1.0.0"}' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        config: '{"theme": "dark", "lang": "en"}',
        metadata: '{"version": "1.0.0"}',
      });
    });

    it('handles tags with very long values', () => {
      const longValue = 'a'.repeat(1000);
      const tags = [
        { key: 'longText', value: longValue },
        { key: 'normal', value: 'short' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        longText: longValue,
        normal: 'short',
      });
    });

    it('handles tags with duplicate keys (last one wins)', () => {
      const tags = [
        { key: 'name', value: 'John' },
        { key: 'name', value: 'Jane' },
        { key: 'name', value: 'Bob' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({ name: 'Bob' });
    });

    it('handles tags with empty keys', () => {
      const tags = [
        { key: '', value: 'empty key value' },
        { key: 'normal', value: 'normal value' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        '': 'empty key value',
        normal: 'normal value',
      });
    });

    it('handles tags with whitespace-only keys', () => {
      const tags = [
        { key: '   ', value: 'whitespace key value' },
        { key: 'normal', value: 'normal value' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        '   ': 'whitespace key value',
        normal: 'normal value',
      });
    });

    it('handles tags with null-like values', () => {
      const tags = [
        { key: 'nullValue', value: 'null' },
        { key: 'undefinedValue', value: 'undefined' },
        { key: 'normal', value: 'normal value' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        nullValue: 'null',
        undefinedValue: 'undefined',
        normal: 'normal value',
      });
    });

    it('handles tags with HTML-like content', () => {
      const tags = [
        { key: 'html', value: '<div>Hello World</div>' },
        { key: 'script', value: '<script>alert("xss")</script>' },
        { key: 'normal', value: 'plain text' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        html: '<div>Hello World</div>',
        script: '<script>alert("xss")</script>',
        normal: 'plain text',
      });
    });

    it('handles tags with newlines and tabs', () => {
      const tags = [
        { key: 'multiline', value: 'line1\nline2\nline3' },
        { key: 'tabbed', value: 'col1\tcol2\tcol3' },
        { key: 'mixed', value: 'start\n\tindented\nend' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        multiline: 'line1\nline2\nline3',
        tabbed: 'col1\tcol2\tcol3',
        mixed: 'start\n\tindented\nend',
      });
    });

    it('handles tags with URL-like values', () => {
      const tags = [
        { key: 'website', value: 'https://example.com' },
        { key: 'api', value: 'http://api.example.com/v1' },
        { key: 'file', value: 'file:///path/to/file.txt' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        website: 'https://example.com',
        api: 'http://api.example.com/v1',
        file: 'file:///path/to/file.txt',
      });
    });

    it('handles tags with email-like values', () => {
      const tags = [
        { key: 'contact', value: 'user@example.com' },
        { key: 'support', value: 'help@company.org' },
        { key: 'admin', value: 'admin@domain.net' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        contact: 'user@example.com',
        support: 'help@company.org',
        admin: 'admin@domain.net',
      });
    });

    it('handles tags with date-like strings', () => {
      const tags = [
        { key: 'created', value: '2023-01-01T00:00:00Z' },
        { key: 'updated', value: '2023-12-31T23:59:59Z' },
        { key: 'due', value: '2024-06-15' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        created: '2023-01-01T00:00:00Z',
        updated: '2023-12-31T23:59:59Z',
        due: '2024-06-15',
      });
    });

    it('handles tags with array-like strings', () => {
      const tags = [
        { key: 'colors', value: '["red", "green", "blue"]' },
        { key: 'numbers', value: '[1, 2, 3, 4, 5]' },
        { key: 'mixed', value: '["text", 42, true, null]' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        colors: '["red", "green", "blue"]',
        numbers: '[1, 2, 3, 4, 5]',
        mixed: '["text", 42, true, null]',
      });
    });

    it('handles tags with object-like strings', () => {
      const tags = [
        { key: 'user', value: '{"id": 1, "name": "John"}' },
        { key: 'settings', value: '{"theme": "dark", "lang": "en"}' },
        { key: 'nested', value: '{"parent": {"child": "value"}}' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        user: '{"id": 1, "name": "John"}',
        settings: '{"theme": "dark", "lang": "en"}',
        nested: '{"parent": {"child": "value"}}',
      });
    });

    it('handles tags with XML-like content', () => {
      const tags = [
        { key: 'xml', value: '<root><item>value</item></root>' },
        { key: 'soap', value: '<soap:Envelope><soap:Body>...</soap:Body></soap:Envelope>' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        xml: '<root><item>value</item></root>',
        soap: '<soap:Envelope><soap:Body>...</soap:Body></soap:Envelope>',
      });
    });

    it('handles tags with SQL-like content', () => {
      const tags = [
        { key: 'query', value: 'SELECT * FROM users WHERE id = 1' },
        { key: 'insert', value: 'INSERT INTO logs (message) VALUES ("test")' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        query: 'SELECT * FROM users WHERE id = 1',
        insert: 'INSERT INTO logs (message) VALUES ("test")',
      });
    });

    it('handles tags with regex-like content', () => {
      const tags = [
        { key: 'pattern', value: '/^[a-zA-Z0-9]+$/' },
        { key: 'emailRegex', value: '/^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        pattern: '/^[a-zA-Z0-9]+$/',
        emailRegex: '/^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/',
      });
    });

    it('handles tags with markdown-like content', () => {
      const tags = [
        { key: 'title', value: '# Main Title' },
        { key: 'list', value: '- Item 1\n- Item 2\n- Item 3' },
        { key: 'link', value: '[Link Text](https://example.com)' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        title: '# Main Title',
        list: '- Item 1\n- Item 2\n- Item 3',
        link: '[Link Text](https://example.com)',
      });
    });

    it('handles tags with base64-like content', () => {
      const tags = [
        { key: 'encoded', value: 'SGVsbG8gV29ybGQ=' },
        {
          key: 'binary',
          value:
            'AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6e3x9fn+AgYKDhIWGh4iJiouMjY6PkJGSk5SVlpeYmZqbnJ2en6ChoqOkpaanqKmqq6ytrq+wsbKztLW2t7i5uru8vb6/wMHCw8TFxsfIycrLzM3Oz9DR0tPU1dbX2Nna29zd3t/g4eLj5OXm5+jp6uvs7e7v8PHy8/T19vf4+fr7/P3+/w==',
        },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        encoded: 'SGVsbG8gV29ybGQ=',
        binary:
          'AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0BBQkNERUZHSElKS0xNTk9QUVJTVFVWV1hZWltcXV5fYGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6e3x9fn+AgYKDhIWGh4iJiouMjY6PkJGSk5SVlpeYmZqbnJ2en6ChoqOkpaanqKmqq6ytrq+wsbKztLW2t7i5uru8vb6/wMHCw8TFxsfIycrLzM3Oz9DR0tPU1dbX2Nna29zd3t/g4eLj5OXm5+jp6uvs7e7v8PHy8/T19vf4+fr7/P3+/w==',
      });
    });

    it('handles tags with very large numbers as strings', () => {
      const tags = [
        { key: 'bigInt', value: '9007199254740991' },
        { key: 'negative', value: '-9007199254740991' },
        { key: 'decimal', value: '3.141592653589793' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        bigInt: '9007199254740991',
        negative: '-9007199254740991',
        decimal: '3.141592653589793',
      });
    });

    it('handles tags with escaped characters', () => {
      const tags = [
        { key: 'quotes', value: 'He said "Hello World"' },
        { key: 'backslash', value: 'C:\\Users\\Username\\Documents' },
        { key: 'newline', value: 'Line 1\\nLine 2' },
      ];
      const result = convertTagsToRecord(tags);
      expect(result).toEqual({
        quotes: 'He said "Hello World"',
        backslash: 'C:\\Users\\Username\\Documents',
        newline: 'Line 1\\nLine 2',
      });
    });
  });
});
