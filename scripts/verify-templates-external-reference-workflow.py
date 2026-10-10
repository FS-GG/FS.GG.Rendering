#!/usr/bin/env python3
"""Verify the literal reusable workflow/checkout join without network or native execution."""
from pathlib import Path
import re

path=Path(__file__).resolve().parents[1]/'.github/workflows/templates-external-reference-qualification.yml'
source=path.read_text()
def validate(text):
    uses=re.findall(r'^    uses: FS-GG/FS.GG.Templates/\.github/workflows/fable-external-reference-source\.yml@([0-9a-f]{40})$',text,re.M)
    checkout=re.findall(r'^      templates-source: ([0-9a-f]{40})$',text,re.M)
    assert len(uses)==len(checkout)==1 and uses==checkout, 'literal workflow/input identity must match'
    assert '  contents: read\n  actions: read' in text
    assert not re.search(r'(contents|actions|packages|id-token): write|secrets:|secrets\.|environment:|continue-on-error|concurrency:',text)
    assert "preflight-only: ${{ github.event_name == 'pull_request' || inputs.preflight-only }}" in text
    assert 'type: boolean\n        default: true' in text
    assert '    needs: static' in text
    assert uses[0] == '775853887a88a3c72e726daa475a49053e84c4bd', 'selected protected Templates source required'
    assert re.findall(r'^      rendering-input-source: (.+)$', text, re.M) == ['public'], 'selected public Rendering input required'
    return uses[0]
head=validate(source)
mutations=[source.replace('      rendering-input-source: public\n',''),source.replace('rendering-input-source: public','rendering-input-source: candidate'),source.replace('rendering-input-source: public','rendering-input-source: unknown'),source.replace('templates-source: '+head,'templates-source: '+'0'*40),source.replace('@'+head,'@main'),source.replace('templates-source: '+head,'templates-source: ${{ github.sha }}'),source.replace('contents: read','contents: write'),source+'\nsecrets: inherit\n',source.replace('default: true','default: false'),source.replace("github.event_name == 'pull_request' || inputs.preflight-only",'inputs.preflight-only'),source+'\nconcurrency: shared\n',source.replace(head,'0'*40)]
mutations.extend([
    source.replace(head,'92944277a8d27b15759dd8e5803acf4c4e0ae162'),
    source.replace(head,'0d945052250eb53056a5b4a87fe742efd1b7a0a2'),
    source.replace(head,'b005c26e43cd4a823577ff94e1ee6da2cfed58d4'),
    source.replace(head,'208e5bffe99153375f7d3e2e1c84653104883cd3'),
    source.replace('@'+head,'@'+'0'*40),
    source.replace('actions: read','actions: write'),
])
for mutation in mutations:
    try: validate(mutation)
    except AssertionError: pass
    else: raise AssertionError('known-bad wrapper accepted')
print(f'PASS exact Templates identity {head}; {len(mutations)} negative wrapper controls')
