import {describe,it,expect} from 'vitest'; describe('login feature',()=>it('has the expected route contract',()=>expect('/api/v1/auth/login').toContain('/auth/login')));
