#!/usr/bin/env python3
"""Finite static caller controls; no network, tools, credentials or workload launch."""
import ast
import json
import os
import sys
from pathlib import Path
import re

HISTORICAL = '6a5dd5305ac6fe26f9f9726c72b1a7e553b0021b'
FONT_DISPOSAL = '752a2ac1fa996c3235ac0330ffbcefb6b8301cb4'
CURRENT_PUBLIC = '59ce6af8b63a816d931e3a2e3f5c1ba890e9427d'
PATH = Path(__file__).resolve().parents[1]/'.github/workflows/templates-external-reference-qualification.yml'

def jobs(text):
    rows=re.findall(r'^  ([A-Za-z_][A-Za-z0-9_-]*):\n(.*?)(?=^  [A-Za-z_][A-Za-z0-9_-]*:|\Z)', text.split('\njobs:\n',1)[1], re.M|re.S)
    assert len(rows)==len(dict(rows)), 'duplicate job key'
    return dict(rows)

def schema(text):
    block=text.split('  workflow_dispatch:\n    inputs:\n',1)[1].split('\npermissions:',1)[0]
    pairs=re.findall(r'^      ([A-Za-z_][A-Za-z0-9_-]*):\n(.*?)(?=^      [A-Za-z_][A-Za-z0-9_-]*:|\Z)',block,re.M|re.S)
    assert len(pairs)==len(dict(pairs)), 'duplicate dispatch input'
    rows=dict(pairs)
    assert set(rows)=={'rendering-input-source','preflight-only'}, 'closed dispatch keys required'
    result={}
    for key,row in rows.items():
        types=re.findall(r'^        type: (\w+)$',row,re.M);defaults=re.findall(r'^        default: (.+)$',row,re.M)
        assert len(types)==len(defaults)==1
        required=re.findall(r'^        required: (.+)$',row,re.M);assert not required or required==['false']
        options=re.findall(r'^          - (.+)$',row,re.M)
        result[key]=(types[0],defaults[0],options)
    assert result=={'rendering-input-source':('choice','candidate',['candidate','font-disposal','current-public-composition']),'preflight-only':('boolean','true',[])}, 'safe dispatch defaults/types/options required'
    return result

def dispatch(text,request,event='workflow_dispatch'):
    assert event in ['pull_request','workflow_dispatch'], 'unsupported event'
    fields=schema(text)
    assert type(request) is dict and set(request)=={'ref','inputs'} and request['ref']=='main', 'exact main dispatch shape required'
    assert type(request['inputs']) is dict and set(request['inputs'])<=set(fields), 'unknown dispatch input'
    values={}
    for key,(kind,default,options) in fields.items():
        value=request['inputs'].get(key, default=='true' if kind=='boolean' else default)
        assert (type(value) is bool if kind=='boolean' else type(value) is str and value in options), 'dispatch type/choice refused'
        values[key]=value
    selected=[]
    for name,body in jobs(text).items():
        if not name.startswith('qualify'):continue
        conditions=re.findall(r'^    if: \$\{\{ (.+) \}\}$',body,re.M);assert len(conditions)==1
        expr=conditions[0].replace('github.event_name',repr(event)).replace('inputs.rendering-input-source',repr(values['rendering-input-source'])).replace('&&',' and ').replace('||',' or ')
        tree=ast.parse(expr,mode='eval')
        assert all(isinstance(n,(ast.Expression,ast.BoolOp,ast.And,ast.Or,ast.Compare,ast.Eq,ast.NotEq,ast.Constant,ast.Load)) for n in ast.walk(tree)), 'unsupported actual job condition'
        if eval(compile(tree,'<actual-job-condition>','eval'),{'__builtins__':{}},{}):selected.append(name)
    assert len(selected)==1, 'exactly one receiver subject required'
    body=jobs(text)[selected[0]]
    preflight=re.findall(r'^      preflight-only: \$\{\{ (.+) \}\}$',body,re.M);assert len(preflight)==1
    assert preflight[0] in ["github.event_name == 'pull_request' || inputs.preflight-only",'inputs.preflight-only']
    full=not (event=='pull_request' and preflight[0].startswith('github.event_name') or values['preflight-only'])
    return selected[0],full

def validate_context(text, environment):
    event=environment.get('QUALIFICATION_EVENT')
    assert event in ['pull_request','workflow_dispatch'], 'actual caller event required'
    if event=='pull_request':
        return dispatch(text,{'ref':'main','inputs':{}},event)
    inputs={'rendering-input-source':environment.get('QUALIFICATION_INPUT_SOURCE'),
            'preflight-only':json.loads(environment.get('QUALIFICATION_PREFLIGHT_JSON','null'))}
    return dispatch(text,{'ref':'main','inputs':inputs},event)

def validate(text):
    assert re.search(r'^permissions:\n  contents: read\n  actions: read\n',text,re.M)
    assert not re.search(r'(contents|actions|packages|id-token): write|secrets:|secrets\.|environment:|continue-on-error|concurrency:',text)
    assert set(jobs(text))=={'static','qualify','qualify-font-disposal','qualify-current-public-composition'}
    assert re.findall(r'^      - run: (.+)$',jobs(text)['static'],re.M)==['python3 scripts/verify-templates-external-reference-workflow.py'], 'production static entrypoint must validate actual context'
    for handoff in ['QUALIFICATION_EVENT: ${{ github.event_name }}','QUALIFICATION_INPUT_SOURCE: ${{ inputs.rendering-input-source }}','QUALIFICATION_PREFLIGHT_JSON: ${{ toJSON(inputs.preflight-only) }}']:
        assert handoff in jobs(text)['static'], 'actual dispatch context handoff required'
    schema(text)
    for name,pin,mode in [('qualify',HISTORICAL,'candidate'),('qualify-font-disposal',FONT_DISPOSAL,'font-disposal'),('qualify-current-public-composition',CURRENT_PUBLIC,'current-public-composition')]:
        body=jobs(text)[name]
        assert re.findall(r'^    uses: FS-GG/FS.GG.Templates/\.github/workflows/fable-external-reference-source\.yml@([0-9a-f]{40})$',body,re.M)==[pin], 'immutable selected workflow pin required'
        assert re.findall(r'^      templates-source: (.+)$',body,re.M)==[pin], 'workflow/checkout identity differs'
        assert re.findall(r'^      rendering-input-source: (.+)$',body,re.M)==[mode], 'closed receiver subject differs'
        assert '    needs: static\n' in body
        if mode == 'current-public-composition':
            assert re.findall(r'^      current-public-at-admission: (.+)$', body, re.M) == ["''"], 'current native admission must remain empty'
    for mode,job in [('candidate','qualify'),('font-disposal','qualify-font-disposal'),('current-public-composition','qualify-current-public-composition')]:
        for full in [False,True]:
            assert dispatch(text,{'ref':'main','inputs':{'rendering-input-source':mode,'preflight-only':not full}})==(job,full)
    assert dispatch(text,{'ref':'main','inputs':{}})==('qualify',False)
    assert dispatch(text,{'ref':'main','inputs':{'rendering-input-source':'font-disposal','preflight-only':False}},'pull_request')==('qualify',False), 'PR must stay historical preflight'
    return text

def controls(text):
    validate(text)
    bad=[text.replace('contents: read','contents: write'),text+'\nsecrets: inherit\n',text.replace('default: true','default: false'),text.replace('default: candidate','default: font-disposal'),text.replace('      templates-source: '+FONT_DISPOSAL,'      templates-source: '+HISTORICAL),text.replace('@'+FONT_DISPOSAL,'@main'),text.replace('rendering-input-source: font-disposal','rendering-input-source: candidate'),text.replace("inputs.rendering-input-source != 'font-disposal'","inputs.rendering-input-source == 'font-disposal'"),text.replace("inputs.rendering-input-source == 'font-disposal'","inputs.rendering-input-source != 'font-disposal'"),text.replace("github.event_name == 'pull_request' || inputs.preflight-only",'inputs.preflight-only'),text.replace('    needs: static\n',''),text.replace(FONT_DISPOSAL,'0'*40),text.replace('        type: boolean','        type: string'),text.replace('        default: true','        default: true\n        required: true'),text+'\n  extra_job:\n    runs-on: ubuntu-latest\n    steps: []\n']
    bad.append(text.replace('      - run: python3 scripts/verify-templates-external-reference-workflow.py\n','      - run: python3 scripts/verify-templates-external-reference-workflow.py --self-test\n'))
    # Preserve the original historical-wrapper mutants as well as new subject routing.
    bad.extend([
        text.replace('      rendering-input-source: candidate\n',''),
        text.replace('rendering-input-source: candidate','rendering-input-source: public'),
        text.replace('rendering-input-source: candidate','rendering-input-source: unknown'),
        text.replace('templates-source: '+HISTORICAL,'templates-source: '+'0'*40),
        text.replace('@'+HISTORICAL,'@main'),
        text.replace('templates-source: '+HISTORICAL,'templates-source: ${{ github.sha }}'),
        text+'\nconcurrency: shared\n',
        text.replace('@'+HISTORICAL,'@'+'0'*40),
        text.replace('actions: read','actions: write'),
    ])
    for rejected in ['0'*40,'775853887a88a3c72e726daa475a49053e84c4bd','92944277a8d27b15759dd8e5803acf4c4e0ae162','0d945052250eb53056a5b4a87fe742efd1b7a0a2','b005c26e43cd4a823577ff94e1ee6da2cfed58d4','208e5bffe99153375f7d3e2e1c84653104883cd3']:
        bad.append(text.replace(HISTORICAL,rejected))
    bad.extend([text.replace(CURRENT_PUBLIC, '0'*40), text.replace('@'+CURRENT_PUBLIC, '@main'), text.replace("current-public-at-admission: ''", "current-public-at-admission: selected"), text.replace(" && inputs.rendering-input-source != 'current-public-composition'", ''), text.replace("inputs.rendering-input-source == 'current-public-composition'", "inputs.rendering-input-source != 'current-public-composition'")])
    for mutation in bad:
        try:validate(mutation)
        except (AssertionError,ValueError,IndexError):pass
        else:raise AssertionError('bad caller mutation accepted')
    for inputs in [{'unknown':False},{'preflight-only':'false'},{'preflight-only':0},{'rendering-input-source':'public'},{'rendering-input-source':'unknown'},{'rendering-input-source':True}]:
        try:dispatch(text,{'ref':'main','inputs':inputs})
        except AssertionError:pass
        else:raise AssertionError('invalid dispatch accepted')
    for request in [{'ref':'other','inputs':{}},{'ref':'main','inputs':{},'extra':False},{'ref':'main','inputs':[]}]:
        try:dispatch(text,request)
        except AssertionError:pass
        else:raise AssertionError('invalid request shape accepted')
    for mode,job in [('candidate','qualify'),('font-disposal','qualify-font-disposal'),('current-public-composition','qualify-current-public-composition')]:
        for phase in [False,True]:
            assert validate_context(text,{'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':mode,'QUALIFICATION_PREFLIGHT_JSON':json.dumps(phase)})==(job,not phase)
    assert validate_context(text,{'QUALIFICATION_EVENT':'pull_request'})==('qualify',False)
    for environment in [{},{'QUALIFICATION_EVENT':'push'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'unknown','QUALIFICATION_PREFLIGHT_JSON':'true'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'font-disposal','QUALIFICATION_PREFLIGHT_JSON':'"false"'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'font-disposal','QUALIFICATION_PREFLIGHT_JSON':'null'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'font-disposal','QUALIFICATION_PREFLIGHT_JSON':'{'}]:
        try:validate_context(text,environment)
        except (AssertionError,ValueError):pass
        else:raise AssertionError('invalid actual dispatch context accepted')
    for key in ['QUALIFICATION_EVENT','QUALIFICATION_INPUT_SOURCE','QUALIFICATION_PREFLIGHT_JSON']:
        mutation=re.sub(r'^          '+key+r':.*\n','',text,flags=re.M)
        try:validate(mutation)
        except AssertionError:pass
        else:raise AssertionError('missing context handoff accepted')
    print(f'PASS actual dispatch schema/defaults, six finite subject/phase branches, PR refusal, {len(bad)+9} caller/request refusals and 9 actual-context/handoff refusals')

if __name__=='__main__':
    assert sys.argv[1:] in [[],['--self-test']], 'unknown verifier option'
    text=PATH.read_text()
    controls(text)
    if not sys.argv[1:]:
        print('actual caller selection:',validate_context(text,os.environ))
