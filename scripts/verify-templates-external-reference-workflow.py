#!/usr/bin/env python3
"""Finite static caller controls; no network, tools, credentials or workload launch."""
import ast
import json
import os
import sys
from pathlib import Path
import re

HISTORICAL = '6a5dd5305ac6fe26f9f9726c72b1a7e553b0021b'
FONT_DISPOSAL = '2f8d400a6a670568044d607a1ac0eb2c7c655a29'
CURRENT_PUBLIC = 'd867ea9216118dc649ce52be6c9bcecdfb46e4e3'
# Retained 0.32.3 current-public callee (Templates 789658f6, #804): its producer tuple is stale.
STALE_CURRENT_PUBLIC_0323 = '789658f6ba487d839567df6ee556c7a21ede2bd8'
PALETTE = 'fe24d30872c84d4c1c92afd7a05c5cf22d00bb10'
# Accepted retained-original evidence (run 40744519859) ran this PR-head Templates commit; it is unreachable from
# any Templates branch since the #806 squash merge, so GitHub reports "workflow was not found" for it.
ORPHANED_PALETTE = '72a54c27f49eff90f1c86bc68e7a8ed8bfd9a431'
PALETTE_PRODUCER = '4ede04be66cedffc8551cd14045bab714aa358a7'
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
    assert set(rows)=={'rendering-input-source','preflight-only','native-input-facility','rendering-successor-input'}, 'closed dispatch keys required'
    result={}
    for key,row in rows.items():
        types=re.findall(r'^        type: (\w+)$',row,re.M);defaults=re.findall(r'^        default: (.+)$',row,re.M)
        assert len(types)==len(defaults)==1
        required=re.findall(r'^        required: (.+)$',row,re.M);assert not required or required==['false']
        options=re.findall(r'^          - (.+)$',row,re.M)
        result[key]=(types[0],defaults[0],options)
    assert result=={'rendering-input-source':('choice','candidate',['candidate','font-disposal','current-public-composition','palette-heading']),'preflight-only':('boolean','true',[]),'native-input-facility':('string',"''",[]),'rendering-successor-input':('string',"''",[])}, 'safe dispatch defaults/types/options required'
    return result

def palette_coordinates(raw):
    assert type(raw) is str and raw.strip(), 'actual acquired producer coordinates required for palette route'
    def unique(pairs):
        result={}
        for key,value in pairs:
            assert key not in result, 'duplicate producer coordinate'
            result[key]=value
        return result
    row=json.loads(raw,object_pairs_hook=unique)
    assert type(row) is dict and set(row)=={'producerSha','producerRun','producerAttempt','producerArtifact','outerSha256','outerSizeBytes','custodySha256'}, 'exact acquired producer coordinates required'
    assert row['producerSha']==PALETTE_PRODUCER, 'selected immutable palette producer required'
    for key in ['producerRun','producerAttempt','producerArtifact','outerSizeBytes']:
        assert type(row[key]) is int and row[key]>0, 'positive exact producer coordinate required'
    assert row['producerAttempt']==1, 'fresh first producer attempt required'
    assert row['outerSizeBytes']<=67108864, 'bounded original archive required'
    for key in ['outerSha256','custodySha256']:assert type(row[key]) is str and re.fullmatch('[0-9a-f]{64}',row[key]), 'exact original digest required'
    return row

# Inert control operands only; never defaults or runtime evidence.
def palette_fixture():
    return json.dumps({'producerSha':PALETTE_PRODUCER,'producerRun':11,'producerAttempt':1,'producerArtifact':12,'outerSha256':'b'*64,'outerSizeBytes':1234,'custodySha256':'c'*64})

def dispatch(text,request,event='workflow_dispatch'):
    assert event in ['pull_request','workflow_dispatch'], 'unsupported event'
    fields=schema(text)
    assert type(request) is dict and set(request)=={'ref','inputs'} and request['ref']=='main', 'exact main dispatch shape required'
    assert type(request['inputs']) is dict and set(request['inputs'])<=set(fields), 'unknown dispatch input'
    values={}
    for key,(kind,default,options) in fields.items():
        fallback = default=='true' if kind=='boolean' else '' if kind=='string' else default
        value=request['inputs'].get(key, fallback)
        assert (type(value) is bool if kind=='boolean' else type(value) is str and
                (len(value.encode('utf-8')) <= 65536 if kind=='string' else value in options)), 'dispatch type/choice/bound refused'
        values[key]=value
    assert not values['native-input-facility'] or values['rendering-input-source']=='current-public-composition', 'facility input belongs only to current-public mode'
    if values['rendering-input-source']=='palette-heading':palette_coordinates(values['rendering-successor-input'])
    else:assert not values['rendering-successor-input'], 'successor coordinates belong only to palette mode'
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
            'preflight-only':json.loads(environment.get('QUALIFICATION_PREFLIGHT_JSON','null')),
            'native-input-facility':environment.get('QUALIFICATION_NATIVE_INPUT_FACILITY'),
            'rendering-successor-input':environment.get('QUALIFICATION_SUCCESSOR_INPUT','')}
    return dispatch(text,{'ref':'main','inputs':inputs},event)

def validate(text):
    assert re.search(r'^permissions:\n  contents: read\n  actions: read\n',text,re.M)
    assert not re.search(r'(contents|actions|packages|id-token): write|secrets:|secrets\.|environment:|continue-on-error|concurrency:',text)
    assert set(jobs(text))=={'static','qualify','qualify-font-disposal','qualify-current-public-composition','qualify-palette-heading'}
    assert re.findall(r'^      - run: (.+)$',jobs(text)['static'],re.M)==['python3 scripts/verify-templates-external-reference-workflow.py'], 'production static entrypoint must validate actual context'
    for handoff in ['QUALIFICATION_EVENT: ${{ github.event_name }}','QUALIFICATION_INPUT_SOURCE: ${{ inputs.rendering-input-source }}','QUALIFICATION_PREFLIGHT_JSON: ${{ toJSON(inputs.preflight-only) }}','QUALIFICATION_NATIVE_INPUT_FACILITY: ${{ inputs.native-input-facility }}','QUALIFICATION_SUCCESSOR_INPUT: ${{ inputs.rendering-successor-input }}']:
        assert handoff in jobs(text)['static'], 'actual dispatch context handoff required'
    assert re.findall(r'^      - name: Every pinned Templates commit is reachable from Templates main\n        run: (.+)\n        env:\n          GH_TOKEN: \$\{\{ github\.token \}\}\n',jobs(text)['static'],re.M)==['python3 scripts/check-templates-pin-reachability.py'], 'Templates pin reachability step required'
    schema(text)
    for name,pin,mode in [('qualify',HISTORICAL,'candidate'),('qualify-font-disposal',FONT_DISPOSAL,'font-disposal'),('qualify-current-public-composition',CURRENT_PUBLIC,'current-public-composition'),('qualify-palette-heading',PALETTE,'palette-heading')]:
        body=jobs(text)[name]
        assert re.findall(r'^    uses: FS-GG/FS.GG.Templates/\.github/workflows/fable-external-reference-source\.yml@([0-9a-f]{40})$',body,re.M)==[pin], 'immutable selected workflow pin required'
        assert re.findall(r'^      templates-source: (.+)$',body,re.M)==[pin], 'workflow/checkout identity differs'
        assert re.findall(r'^      rendering-input-source: (.+)$',body,re.M)==[mode], 'closed receiver subject differs'
        assert '    needs: static\n' in body
        if mode == 'current-public-composition':
            assert re.findall(r'^      current-public-at-admission: (.+)$', body, re.M) == ["''"], 'current native admission must remain empty'
            assert re.findall(r'^      native-input-facility: (.+)$',body,re.M)==['${{ inputs.native-input-facility }}'], 'exact opaque facility handoff required'
        else:
            assert 'native-input-facility:' not in body, 'historical callee cannot receive new facility input'
        if mode=='palette-heading':assert re.findall(r'^      rendering-successor-input: (.+)$',body,re.M)==['${{ inputs.rendering-successor-input }}'], 'exact opaque producer handoff required'
        else:assert 'rendering-successor-input:' not in body, 'other routes cannot receive successor coordinates'
    for mode,job in [('candidate','qualify'),('font-disposal','qualify-font-disposal'),('current-public-composition','qualify-current-public-composition')]:
        for full in [False,True]:
            assert dispatch(text,{'ref':'main','inputs':{'rendering-input-source':mode,'preflight-only':not full}})==(job,full)
    for full in [False,True]:
        assert dispatch(text,{'ref':'main','inputs':{'rendering-input-source':'palette-heading','rendering-successor-input':palette_fixture(),'preflight-only':not full}})==('qualify-palette-heading',full)
    assert dispatch(text,{'ref':'main','inputs':{}})==('qualify',False)
    assert dispatch(text,{'ref':'main','inputs':{'rendering-input-source':'font-disposal','preflight-only':False}},'pull_request')==('qualify',False), 'PR must stay historical preflight'
    return text

def controls(text):
    validate(text)
    bad=[text.replace('contents: read','contents: write'),text+'\nsecrets: inherit\n',text.replace('default: true','default: false'),text.replace('default: candidate','default: font-disposal'),text.replace('      templates-source: '+FONT_DISPOSAL,'      templates-source: '+HISTORICAL),text.replace('@'+FONT_DISPOSAL,'@main'),text.replace('rendering-input-source: font-disposal','rendering-input-source: candidate'),text.replace("inputs.rendering-input-source != 'font-disposal'","inputs.rendering-input-source == 'font-disposal'"),text.replace("inputs.rendering-input-source == 'font-disposal'","inputs.rendering-input-source != 'font-disposal'"),text.replace("github.event_name == 'pull_request' || inputs.preflight-only",'inputs.preflight-only'),text.replace('    needs: static\n',''),text.replace(FONT_DISPOSAL,'0'*40),text.replace('        type: boolean','        type: string'),text.replace('        default: true','        default: true\n        required: true'),text+'\n  extra_job:\n    runs-on: ubuntu-latest\n    steps: []\n']
    bad.extend([text.replace('        run: python3 scripts/check-templates-pin-reachability.py\n','        run: true\n'),
                text.replace('      - name: Every pinned Templates commit is reachable from Templates main\n        run: python3 scripts/check-templates-pin-reachability.py\n        env:\n          GH_TOKEN: ${{ github.token }}\n',''),
                text.replace('scripts/check-templates-pin-reachability.py','scripts/other.py')])
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
    # Original40172407851 failed eager GHA resolution of this API-readable intermediate pin.
    bad.append(text.replace(FONT_DISPOSAL, '752a2ac1fa996c3235ac0330ffbcefb6b8301cb4'))
    bad.extend([text.replace(CURRENT_PUBLIC, STALE_CURRENT_PUBLIC_0323), text.replace('@'+CURRENT_PUBLIC, '@'+STALE_CURRENT_PUBLIC_0323), text.replace('templates-source: '+CURRENT_PUBLIC, 'templates-source: '+STALE_CURRENT_PUBLIC_0323), text.replace(CURRENT_PUBLIC, '0'*40), text.replace('@'+CURRENT_PUBLIC, '@main'), text.replace("current-public-at-admission: ''", "current-public-at-admission: selected"), text.replace(" && inputs.rendering-input-source != 'current-public-composition'", ''), text.replace("inputs.rendering-input-source == 'current-public-composition'", "inputs.rendering-input-source != 'current-public-composition'")])
    bad.extend([text.replace(CURRENT_PUBLIC,'59ce6af8b63a816d931e3a2e3f5c1ba890e9427d'),
        text.replace(CURRENT_PUBLIC,'3ececb44da3f8498ac9a295f67a7dffd10964a36'),
        text.replace('native-input-facility: ${{ inputs.native-input-facility }}','native-input-facility: fixed'),
        text.replace("        default: ''","        default: supplied"),
        text.replace('          QUALIFICATION_NATIVE_INPUT_FACILITY: ${{ inputs.native-input-facility }}\n','')])
    # Both phases select one immutable receiver; malformed/later-attempt tuples stop in static.
    palette_refusals=0
    for phase in [False,True]:
        env={'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'palette-heading','QUALIFICATION_PREFLIGHT_JSON':json.dumps(phase),'QUALIFICATION_NATIVE_INPUT_FACILITY':'','QUALIFICATION_SUCCESSOR_INPUT':palette_fixture()}
        assert validate_context(text,env)==('qualify-palette-heading',not phase)
    assert dispatch(text,{'ref':'main','inputs':{'rendering-input-source':'palette-heading','rendering-successor-input':palette_fixture(),'preflight-only':False}},'pull_request')==('qualify',False)
    payloads=['','{}','null','[]','{']
    for key,value in [('producerSha',CURRENT_PUBLIC),('producerRun',True),('producerAttempt',2),('producerAttempt',0),('producerArtifact','12'),('outerSizeBytes',67108865),('outerSha256','bad'),('custodySha256','A'*64)]:
        row=json.loads(palette_fixture());row[key]=value;payloads.append(json.dumps(row))
    payloads.append('{"producerAttempt":1,'+palette_fixture()[1:])
    for payload in payloads:
        try:dispatch(text,{'ref':'main','inputs':{'rendering-input-source':'palette-heading','rendering-successor-input':payload}})
        except (AssertionError,ValueError):palette_refusals+=1
        else:raise AssertionError('invalid acquired palette selection accepted')
    for mode in ['candidate','font-disposal','current-public-composition']:
        try:dispatch(text,{'ref':'main','inputs':{'rendering-input-source':mode,'rendering-successor-input':palette_fixture()}})
        except AssertionError:palette_refusals+=1
        else:raise AssertionError('palette coordinates leaked into another subject')
    bad.extend([text.replace(PALETTE,ORPHANED_PALETTE), text.replace('@'+PALETTE,'@'+ORPHANED_PALETTE), text.replace('templates-source: '+PALETTE,'templates-source: '+ORPHANED_PALETTE), text.replace('@'+PALETTE,'@main'),text.replace('templates-source: '+PALETTE,'templates-source: '+CURRENT_PUBLIC),
        text.replace('rendering-successor-input: ${{ inputs.rendering-successor-input }}','rendering-successor-input: fixed'),
        text.replace(" && inputs.rendering-input-source != 'palette-heading'",''),
        text.replace("inputs.rendering-input-source == 'palette-heading'","inputs.rendering-input-source != 'palette-heading'"),
        text.replace('          QUALIFICATION_SUCCESSOR_INPUT: ${{ inputs.rendering-successor-input }}\n','')])
    for payload in ['', '{"source":"exact opaque fixture"}', 'x'*65536, 'é'*32768]:
        for phase in [False,True]:
            request={'ref':'main','inputs':{'rendering-input-source':'current-public-composition','preflight-only':phase,'native-input-facility':payload}}
            assert dispatch(text,request)==('qualify-current-public-composition',not phase)
            assert validate_context(text,{'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'current-public-composition',
                'QUALIFICATION_PREFLIGHT_JSON':json.dumps(phase),'QUALIFICATION_NATIVE_INPUT_FACILITY':payload})==('qualify-current-public-composition',not phase)
    native_refusals=0
    for mode,payload in [('candidate','{}'),('font-disposal','{}'),('current-public-composition',True),
                         ('current-public-composition',{}),('current-public-composition',None),
                         ('current-public-composition','x'*65537),('current-public-composition','é'*32769)]:
        try:dispatch(text,{'ref':'main','inputs':{'rendering-input-source':mode,'native-input-facility':payload}})
        except AssertionError:native_refusals+=1
        else:raise AssertionError('invalid facility boundary accepted')
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
            assert validate_context(text,{'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':mode,'QUALIFICATION_PREFLIGHT_JSON':json.dumps(phase),'QUALIFICATION_NATIVE_INPUT_FACILITY':''})==(job,not phase)
    assert validate_context(text,{'QUALIFICATION_EVENT':'pull_request'})==('qualify',False)
    bad_contexts = [{},{'QUALIFICATION_EVENT':'push'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'unknown','QUALIFICATION_PREFLIGHT_JSON':'true'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'font-disposal','QUALIFICATION_PREFLIGHT_JSON':'"false"'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'font-disposal','QUALIFICATION_PREFLIGHT_JSON':'null'}, {'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'font-disposal','QUALIFICATION_PREFLIGHT_JSON':'{'}]
    for environment in bad_contexts:
        environment['QUALIFICATION_NATIVE_INPUT_FACILITY']=''
        try:validate_context(text,environment)
        except (AssertionError,ValueError):pass
        else:raise AssertionError('invalid actual dispatch context accepted')
    try:validate_context(text,{'QUALIFICATION_EVENT':'workflow_dispatch','QUALIFICATION_INPUT_SOURCE':'current-public-composition','QUALIFICATION_PREFLIGHT_JSON':'true'})
    except AssertionError:pass
    else:raise AssertionError('missing actual facility context became default')
    for key in ['QUALIFICATION_EVENT','QUALIFICATION_INPUT_SOURCE','QUALIFICATION_PREFLIGHT_JSON','QUALIFICATION_NATIVE_INPUT_FACILITY','QUALIFICATION_SUCCESSOR_INPUT']:
        mutation=re.sub(r'^          '+key+r':.*\n','',text,flags=re.M)
        try:validate(mutation)
        except AssertionError:pass
        else:raise AssertionError('missing context handoff accepted')
    print(f'PASS actual dispatch schema/defaults, eight finite subject/phase branches, PR refusal, {len(bad)+9} caller/request refusals and 12 actual-context/handoff refusals; {native_refusals} facility type/UTF8/scope refusals; {palette_refusals} malformed/attempt/scope palette refusals')

if __name__=='__main__':
    assert sys.argv[1:] in [[],['--self-test']], 'unknown verifier option'
    text=PATH.read_text()
    controls(text)
    if not sys.argv[1:]:
        print('actual caller selection:',validate_context(text,os.environ))
