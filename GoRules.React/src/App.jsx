import { useEffect, useMemo, useState } from 'react';

const emptyRule = {
  id: null,
  ruleName: '',
  description: '',
  isActive: true,
  conditions: [
    { leftOperand: 'PD_Appr_Appraisal_Amt', operator: '>=', rightOperand: '1080000', logicalOperator: 'AND' },
    { leftOperand: 'PD_Doc_Appraisal1_DocumentStatus', operator: '==', rightOperand: 'SUCCESSFUL', logicalOperator: 'AND' },
  ],
  resultDecision: 'Approve',
  resultMessage: 'Eligible for processing',
};

const optionLabels = {
  '>': 'Greater than',
  '<': 'Less than',
  '>=': 'Greater than or equal',
  '<=': 'Less than or equal',
  '==': 'Equals',
  '!=': 'Not equal',
};

const payloadPresets = [
  {
    label: 'Approved collateral',
    value: {
      PD_Doc_Date: '01/21/2026',
      PD_Doc_Appraised_EstimatedPropValue_Amt: 1080000.00,
      PD_Doc_Prop_Address_Zip: 91321,
      PD_Doc_Prop_Address_Street: '23726 LA SALLE CANYON RD',
      PD_Doc_Prop_Address_State: 'CA',
      PD_Doc_Prop_Address_City: 'NEWHALL',
      PD_Doc_DocFileID: '110252D6CC',
      PD_Doc_CollateralUnderwriting_RiskScore: 1,
      PD_Doc_Appraisal1_DocumentStatus: 'SUCCESSFUL',
      PD_Appr_Appraiser_SubjectProp_AppraisedValue: 1080000,
      PD_Appr_Appraiser_State_CertificationNumber: 'AR017444',
      PD_Appr_Appraiser_Name_Last: 'AVEDISSIAN',
      PD_Appr_Appraiser_Name_First: 'VREJ',
      PD_Appr_Appraiser_ExpirationDateOfCertificationOrLicense: '03/24/2026',
      PD_Appr_Appraiser_EffectiveDateOfAppraisal: '01/18/2026',
      PD_Appr_Appraiser_Company_Name: 'SCV INC',
      PD_Appr_Appraiser_Company_Address_Zip: 91326,
      PD_Appr_Appraiser_Company_Address_Street: '12008 EDDLESTON DRIVE,',
      PD_Appr_Appraiser_Company_Address_State: 'CA',
      PD_Appr_Appraiser_Company_Address_City: 'NORTHRIDGE',
      PD_Appr_ProjectInfo_UnitType: 'DETACHED',
      PD_Appr_Subject_RoomCount_Bdrms: 3,
      PD_Appr_Subject_QualityOfConstruction_Desc: 'Q3',
      PD_Appr_Subject_Condition: 'C2',
      PD_Appr_Appraisal_Date: '01/18/2026',
      PD_Appr_Appraisal_Amt: 1080000,
      PD_Appr_FannieMae_FormNumber: '1004',
      PD_Appr_ProjectInfo_TotalNumberOfUnits: 1004,
      PD_Appr_GeneralDesc_YearBuilt: 1979,
      PD_Appr_GeneralDesc_StructureType_2: 'EXISTING',
      PD_Appr_GeneralDesc_StructureType_1: 'DET',
      PD_Appr_LegalDesc_LotNumber: 7,
      PD_Doc_Borr1_Name_Last: 'TOLDEO,',
      PD_Doc_Borr1_Name_First: 'MARVIN',
      PD_Appr_PropRightsAppraised: 'FEE SIMPLE',
      PD_Appr_ParcelNumber: '2827-033-007',
      PD_Appr_NeighborhoodName: 'RANCHO LA SALLE HOA',
      PD_Appr_County: 'LOS ANGELES',
    },
  },
  {
    label: 'Review - risk elevated',
    value: {
      PD_Doc_CollateralUnderwriting_RiskScore: 4,
      PD_Doc_Appraisal1_DocumentStatus: 'SUCCESSFUL',
      PD_Appr_Appraisal_Amt: 1100000,
      PD_Doc_Prop_Address_State: 'CA',
      PD_Doc_Prop_Address_City: 'NEWHALL',
      PD_Doc_Prop_Address_Zip: 91321,
      PD_Doc_DocFileID: '110252D6CC',
      PD_Appr_Appraiser_State_CertificationNumber: 'AR017444',
      PD_Appr_Appraiser_ExpirationDateOfCertificationOrLicense: '03/24/2026',
    },
  },
  {
    label: 'Reject - high risk',
    value: {
      PD_Doc_CollateralUnderwriting_RiskScore: 9,
      PD_Doc_Prop_Address_State: 'CA',
      PD_Doc_Prop_Address_City: 'NEWHALL',
      PD_Doc_Prop_Address_Zip: 91321,
      PD_Doc_DocFileID: '110252D6CC',
      PD_Appr_Appraisal_Amt: 1080000,
      PD_Doc_Appraisal1_DocumentStatus: 'SUCCESSFUL',
    },
  },
  {
    label: 'Missing required value',
    value: {
      PD_Doc_Date: '01/21/2026',
      PD_Doc_Prop_Address_State: 'CA',
      PD_Doc_Prop_Address_City: 'NEWHALL',
      PD_Doc_DocFileID: '110252D6CC',
      PD_Doc_CollateralUnderwriting_RiskScore: 1,
      PD_Doc_Appraisal1_DocumentStatus: 'SUCCESSFUL',
    },
  },
];

function App({ apiUrl }) {
  const API_URL = apiUrl;
  const [page, setPage] = useState('dashboard');
  const [rules, setRules] = useState([]);
  const [dashboard, setDashboard] = useState({ totalRules: 0, activeRules: 0, executionCount: 0 });
  const [selectedRuleId, setSelectedRuleId] = useState(null);
  const [search, setSearch] = useState('');
  const [ruleForm, setRuleForm] = useState(emptyRule);
  const [executionInput, setExecutionInput] = useState(JSON.stringify(payloadPresets[0].value, null, 2));
  const [executionResult, setExecutionResult] = useState(null);
  const [executionInputPreset, setExecutionInputPreset] = useState(payloadPresets[0].label);
  const [executionValidationError, setExecutionValidationError] = useState('');
  const [ruleRunResults, setRuleRunResults] = useState([]);
  const [traceRuleId, setTraceRuleId] = useState(null);
  const [engineDebug, setEngineDebug] = useState({ decisionJson: '', engineResultJson: '' });
  const [loading, setLoading] = useState(false);
  const [selectedRuleIds, setSelectedRuleIds] = useState([]);
  const [tracePopup, setTracePopup] = useState(null);
  const [lastExecutionTrace, setLastExecutionTrace] = useState(null);

  const activeRules = useMemo(() => rules.filter((rule) => rule.isActive), [rules]);

  const operandOptions = useMemo(() => {
    const values = new Set();

    payloadPresets.forEach((preset) => {
      Object.keys(preset.value).forEach((fieldName) => values.add(fieldName));
    });

    rules.forEach((rule) => {
      rule.conditions?.forEach((condition) => {
        if (condition.leftOperand) {
          values.add(condition.leftOperand);
        }
      });
    });

    return Array.from(values).sort();
  }, [rules]);

  const filteredRules = useMemo(() => {
    const value = search.trim().toLowerCase();
    return rules.filter((rule) => {
      if (!value) return true;
      return rule.ruleName.toLowerCase().includes(value) || rule.description.toLowerCase().includes(value);
    });
  }, [rules, search]);

  const refresh = async () => {
    const [ruleList, stats] = await Promise.all([
      fetch(`${API_URL}/rules`).then((r) => r.json()),
      fetch(`${API_URL}/dashboard`).then((r) => r.json()),
    ]);

    setRules(ruleList);
    setDashboard(stats);
  };

  useEffect(() => {
    refresh();
  }, []);

  useEffect(() => {
    if (rules.length > 0 && !selectedRuleId) {
      setSelectedRuleId(rules[0].id);
    }
  }, [rules, selectedRuleId]);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;
    setRuleForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : name === 'isActive' ? value === 'true' : value,
    }));
  };

  const updateCondition = (index, field, value) => {
    setRuleForm((current) => ({
      ...current,
      conditions: current.conditions.map((condition, conditionIndex) =>
        conditionIndex === index ? { ...condition, [field]: value } : condition,
      ),
    }));
  };

  const addCondition = () => {
    setRuleForm((current) => ({
      ...current,
      conditions: [
        ...current.conditions,
        { leftOperand: 'DebtRatio', operator: '>', rightOperand: '40', logicalOperator: 'AND' },
      ],
    }));
  };

  const removeCondition = (index) => {
    setRuleForm((current) => ({
      ...current,
      conditions: current.conditions.filter((_, conditionIndex) => conditionIndex !== index),
    }));
  };

  const resetRule = () => {
    setSelectedRuleId(null);
    setRuleForm(emptyRule);
  };

  const clearExecutionState = () => {
    setSelectedRuleIds([]);
    setRuleRunResults([]);
    setExecutionResult(null);
    setLastExecutionTrace(null);
    setTracePopup(null);
    setTraceRuleId(null);
    setEngineDebug({ decisionJson: '', engineResultJson: '' });
  };

  const validateExecutionInput = (rawValue) => {
    if (!rawValue || !rawValue.trim()) {
      return 'Input JSON is required.';
    }

    try {
      const parsed = JSON.parse(rawValue);
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
        return 'Input JSON must be an object.';
      }
      return '';
    } catch (error) {
      return 'Input JSON is invalid. Please provide a valid JSON object.';
    }
  };

  const isSuccessfulDecision = (decision) => {
    if (!decision) {
      return false;
    }

    return !['No Match', 'Reject'].includes(decision);
  };

  const saveRule = async () => {
    setLoading(true);
    try {
      const payload = {
        id: ruleForm.id,
        ruleName: ruleForm.ruleName,
        description: ruleForm.description,
        isActive: ruleForm.isActive,
        conditions: ruleForm.conditions.map((condition, index) => ({
          ...condition,
          logicalOperator: index === 0 ? 'AND' : condition.logicalOperator,
          displayOrder: index + 1,
        })),
        result: {
          decision: ruleForm.resultDecision,
          message: ruleForm.resultMessage,
        },
      };

      const method = ruleForm.id ? 'PUT' : 'POST';
      const url = ruleForm.id ? `${API_URL}/rules/${ruleForm.id}` : `${API_URL}/rules`;

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error('Unable to save rule');
      }

      const savedRule = await response.json();
      setSelectedRuleId(savedRule.id);
      setRuleForm({
        ...emptyRule,
        ...savedRule,
        conditions: savedRule.conditions.length ? savedRule.conditions : emptyRule.conditions,
        resultDecision: savedRule.result?.decision ?? 'Approve',
        resultMessage: savedRule.result?.message ?? 'Eligible for processing',
      });

      await refresh();
      setPage('rules');
    } finally {
      setLoading(false);
    }
  };

  const editRule = (rule) => {
    setSelectedRuleId(rule.id);
    setRuleForm({
      id: rule.id,
      ruleName: rule.ruleName,
      description: rule.description,
      isActive: rule.isActive,
      conditions: rule.conditions.map((condition) => ({
        leftOperand: condition.leftOperand,
        operator: condition.operator,
        rightOperand: String(condition.rightOperand),
        logicalOperator: condition.logicalOperator,
      })),
      resultDecision: rule.result?.decision ?? 'Approve',
      resultMessage: rule.result?.message ?? 'Eligible for processing',
    });
    setPage('builder');
  };

  const deleteRule = async (id) => {
    const response = await fetch(`${API_URL}/rules/${id}`, { method: 'DELETE' });
    if (response.ok) {
      await refresh();
      if (selectedRuleId === id) resetRule();
    }
  };

  const prettyJson = (value) => {
    if (value === null || value === undefined || value === '') return '';

    const normalizeJsonString = (input) => {
      if (typeof input !== 'string') {
        return input;
      }

      return input
        .replace(/\\r/g, '\r')
        .replace(/\\n/g, '\n')
        .replace(/\\t/g, '\t')
        .replace(/\\"/g, '"')
        .replace(/\\\\/g, '\\');
    };

    const normalizeJsonValue = (input) => {
      if (typeof input === 'string') {
        const trimmed = input.trim();
        if (!trimmed) {
          return input;
        }

        if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
          try {
            return JSON.parse(trimmed);
          } catch {
            try {
              return JSON.parse(normalizeJsonString(trimmed));
            } catch {
              return normalizeJsonString(trimmed);
            }
          }
        }

        return normalizeJsonString(trimmed);
      }

      if (Array.isArray(input)) {
        return input.map((item) => normalizeJsonValue(item));
      }

      if (typeof input === 'object') {
        return Object.fromEntries(
          Object.entries(input).map(([key, item]) => [key, normalizeJsonValue(item)]),
        );
      }

      return input;
    };

    const normalized = normalizeJsonValue(value);

    if (typeof normalized === 'string') {
      return normalized;
    }

    return JSON.stringify(normalized, null, 2);
  };

  const toggleRuleSelection = (ruleId) => {
    setSelectedRuleIds((current) =>
      current.includes(ruleId)
        ? current.filter((id) => id !== ruleId)
        : [...current, ruleId],
    );
  };

  const toggleSelectAll = () => {
    const activeIds = activeRules.map((rule) => rule.id);
    const allSelected = activeIds.length > 0 && activeIds.every((id) => selectedRuleIds.includes(id));

    setSelectedRuleIds((current) => {
      if (allSelected) {
        return current.filter((id) => !activeIds.includes(id));
      }

      return Array.from(new Set([...current, ...activeIds]));
    });
  };

  const executeSelectedRule = async (ruleId = null) => {
    const validationError = validateExecutionInput(executionInput);
    if (validationError) {
      setExecutionValidationError(validationError);
      return null;
    }
    setExecutionValidationError('');

    const effectiveRuleId = ruleId ?? selectedRuleId ?? rules[0]?.id ?? null;
    let body;

    if (effectiveRuleId) {
      body = JSON.stringify({ ruleId: effectiveRuleId, input: JSON.parse(executionInput) });
    } else {
      body = JSON.stringify({ input: JSON.parse(executionInput) });
    }

    const response = await fetch(`${API_URL}/rules/execute`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body,
    });

    const payload = await response.json();
    setExecutionResult(payload);
    setEngineDebug({
      decisionJson: payload.decisionJson ?? '',
      engineResultJson: payload.engineResultJson ?? '',
    });
    return payload;
  };

  const runSelectedRules = async () => {
    if (selectedRuleIds.length === 0) {
      setExecutionValidationError('Select at least one rule before running the evaluation.');
      return;
    }

    const validationError = validateExecutionInput(executionInput);
    if (validationError) {
      setExecutionValidationError(validationError);
      return;
    }
    setExecutionValidationError('');

    const input = JSON.parse(executionInput);
    const isSingleSelection = selectedRuleIds.length === 1;
    const endpoint = isSingleSelection ? `${API_URL}/rules/execute` : `${API_URL}/rules/execute-all`;
    const requestBody = isSingleSelection
      ? { ruleId: selectedRuleIds[0], input }
      : { input, ruleIds: selectedRuleIds };

    const response = await fetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestBody),
    });

    const payload = await response.json();
    const batchResults = Array.isArray(payload)
      ? payload
      : (Array.isArray(payload.results) ? payload.results : [payload]);

    const results = batchResults.map((item) => ({
      ruleId: rules.find((rule) => rule.ruleName === item.matchedRule)?.id ?? selectedRuleIds[0] ?? null,
      ruleName: item.matchedRule,
      decision: item.decision,
      success: isSuccessfulDecision(item.decision),
      message: item.message,
      executionTimeMs: item.executionTimeMs,
      input: item.input,
      decisionJson: item.decisionJson,
      engineResultJson: item.engineResultJson,
    }));

    const firstResult = batchResults[0] ?? payload;
    const normalizedFirstResult = firstResult?.results?.[0] ?? firstResult;

    setRuleRunResults(results);
    setExecutionResult(normalizedFirstResult ?? null);
    setTraceRuleId(results[0]?.ruleId ?? selectedRuleIds[0] ?? null);
    setEngineDebug({
      decisionJson: normalizedFirstResult?.decisionJson ?? payload?.combinedDecisionJson ?? '',
      engineResultJson: normalizedFirstResult?.engineResultJson ?? payload?.combinedEngineResultJson ?? '',
    });

    const calls = [{
      callId: 1,
      apiName: isSingleSelection
        ? 'POST /api/rules/execute (single rule)'
        : 'POST /api/rules/execute-all (combined batch)',
      requestPayload: requestBody,
      goRulesPayload: payload?.combinedDecisionJson ?? normalizedFirstResult?.decisionJson ?? '',
      goRulesResponse: payload?.combinedEngineResultJson ?? normalizedFirstResult?.engineResultJson ?? '',
      response: payload,
      finalBatchResult: payload?.results ? payload : { results: results.map((item) => ({
        decision: item.decision,
        matchedRule: item.ruleName,
        message: item.message,
        executionTimeMs: item.executionTimeMs,
        input: item.input,
        decisionJson: item.decisionJson,
        engineResultJson: item.engineResultJson,
      })) },
    }];

    setLastExecutionTrace({
      apiName: endpoint,
      request: requestBody,
      response: payload,
      requestType: isSingleSelection ? 'single' : 'multi',
      selectedRuleIds,
      calls,
      goRulesPayload: [{
        callId: 1,
        apiName: calls[0].apiName,
        goRulesPayload: calls[0].goRulesPayload,
        goRulesResponse: calls[0].goRulesResponse,
        finalBatchResult: calls[0].finalBatchResult,
      }],
    });
  };

  const renderDashboard = (
    <>
      <div className="kpis">
        <div className="kpi">
          <span className="label">Total Rules</span>
          <span className="value">{dashboard.totalRules}</span>
        </div>
        <div className="kpi">
          <span className="label">Active Rules</span>
          <span className="value">{dashboard.activeRules}</span>
        </div>
        <div className="kpi">
          <span className="label">Execution Count</span>
          <span className="value">{dashboard.executionCount}</span>
        </div>
      </div>

      <div className="card">
        <h3>What this demo shows</h3>
        <ul>
          <li>Rules are created in the UI and stored as metadata.</li>
          <li>Each rule is transformed into a GoRules decision graph JSON.</li>
          <li>The API calls GoRules Zen Engine with the generated JSON and input context.</li>
          <li>Decision output is returned to the UI and displayed in real time.</li>
        </ul>
      </div>
    </>
  );

  const renderRules = (
    <div className="card">
      <div className="inline" style={{ justifyContent: 'space-between', marginBottom: 16 }}>
        <input
          type="text"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search rules"
          style={{ minWidth: 260 }}
        />
        <button className="primary" onClick={() => { resetRule(); setPage('builder'); }}>
          Create rule
        </button>
      </div>

      <table className="table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Description</th>
            <th>Status</th>
            <th>Conditions</th>
            <th>Result</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {filteredRules.map((rule) => (
            <tr key={rule.id}>
              <td>{rule.ruleName}</td>
              <td>{rule.description}</td>
              <td>{rule.isActive ? 'Active' : 'Inactive'}</td>
              <td>{rule.conditions.length}</td>
              <td>{rule.result?.decision ?? '—'}</td>
              <td>
                <div className="actions">
                  <button className="secondary" onClick={() => editRule(rule)}>Edit</button>
                  <button className="danger" onClick={() => deleteRule(rule.id)}>Delete</button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  const renderBuilder = (
    <div className="card">
      <div className="form-grid">
        <div className="field">
          <label>Rule Name</label>
          <input name="ruleName" value={ruleForm.ruleName} onChange={handleChange} placeholder="Credit Score Rule" />
        </div>

        <div className="field">
          <label>Status</label>
          <select name="isActive" value={ruleForm.isActive} onChange={handleChange}>
            <option value={true}>Active</option>
            <option value={false}>Inactive</option>
          </select>
        </div>
      </div>

      <div className="field">
        <label>Description</label>
        <textarea name="description" value={ruleForm.description} onChange={handleChange} rows={3} />
      </div>

      <div className="conditions-box">
        <h3>Conditions</h3>
        {ruleForm.conditions.map((condition, index) => (
          <div key={`${condition.leftOperand}-${index}`} className="condition-row">
            <div className="field">
              <label>Left Operand</label>
              <select value={condition.leftOperand} onChange={(event) => updateCondition(index, 'leftOperand', event.target.value)}>
                {operandOptions.map((operand) => (
                  <option key={operand} value={operand}>{operand}</option>
                ))}
              </select>
            </div>

            <div className="field">
              <label>Operator</label>
              <select value={condition.operator} onChange={(event) => updateCondition(index, 'operator', event.target.value)}>
                {Object.keys(optionLabels).map((operator) => (
                  <option key={operator} value={operator}>{optionLabels[operator]}</option>
                ))}
              </select>
            </div>

            <div className="field">
              <label>Right Operand</label>
              <input list="operand-options" value={condition.rightOperand} onChange={(event) => updateCondition(index, 'rightOperand', event.target.value)} />
            </div>

            <div className="field">
              <label>Logical Operator</label>
              <select value={condition.logicalOperator} onChange={(event) => updateCondition(index, 'logicalOperator', event.target.value)}>
                <option value="AND">AND</option>
                <option value="OR">OR</option>
              </select>
            </div>

            {ruleForm.conditions.length > 1 && (
              <button type="button" className="danger" onClick={() => removeCondition(index)}>
                Remove
              </button>
            )}
          </div>
        ))}

        <div className="inline" style={{ marginTop: 12 }}>
          <button type="button" className="secondary" onClick={addCondition}>Add condition</button>
        </div>
      </div>

      <datalist id="operand-options">
        {operandOptions.map((operand) => (
          <option key={operand} value={operand} />
        ))}
      </datalist>

      <div className="form-grid" style={{ marginTop: 20 }}>
        <div className="field">
          <label>Decision</label>
          <input name="resultDecision" value={ruleForm.resultDecision} onChange={handleChange} />
        </div>
        <div className="field">
          <label>Message</label>
          <input name="resultMessage" value={ruleForm.resultMessage} onChange={handleChange} />
        </div>
      </div>

      <div className="summary-box">
        <strong>Rule preview:</strong> {ruleForm.conditions.map((condition, index) => (
          <span key={`${condition.leftOperand}-${index}`}>
            {index > 0 ? ` ${condition.logicalOperator} ` : ''}
            {condition.leftOperand} {condition.operator} {condition.rightOperand}
          </span>
        ))} = {ruleForm.resultDecision}
      </div>

      <div className="inline" style={{ marginTop: 18 }}>
        <button className="primary" onClick={saveRule} disabled={loading}>
          {loading ? 'Saving...' : 'Save rule'}
        </button>
        <button className="secondary" onClick={resetRule}>Reset</button>
      </div>
    </div>
  );

  const renderExecute = (
    <div className="card">
      <h3>Execute Rule</h3>
      <div className="field">
        <label>Sample input payload</label>
        <select
          value={executionInputPreset}
          onChange={(event) => {
            const selectedPreset = payloadPresets.find((item) => item.label === event.target.value);
            setExecutionInputPreset(event.target.value);
            setExecutionInput(JSON.stringify(selectedPreset?.value ?? {}, null, 2));
            setExecutionValidationError('');
          }}
          style={{ marginBottom: 12 }}
        >
          {payloadPresets.map((item) => (
            <option key={item.label} value={item.label}>{item.label}</option>
          ))}
        </select>
      </div>

      <div className="field">
        <label>Input JSON</label>
        <textarea rows={12} value={executionInput} onChange={(event) => {
          setExecutionInput(event.target.value);
          setExecutionValidationError(validateExecutionInput(event.target.value));
        }} />
      </div>

      {executionValidationError && (
        <div style={{ color: '#b91c1c', marginBottom: 12, fontWeight: 600 }}>{executionValidationError}</div>
      )}

      <div className="inline">
        <button
          className="primary"
          onClick={runSelectedRules}
          disabled={selectedRuleIds.length === 0 || !!executionValidationError}
        >
          {selectedRuleIds.length > 0 ? 'Run selected rules' : 'Select rules to execute'}
        </button>
        <button
          className="secondary"
          onClick={() => {
            clearExecutionState();
            setExecutionInput(JSON.stringify(payloadPresets[0].value, null, 2));
            setExecutionInputPreset(payloadPresets[0].label);
            setExecutionValidationError('');
          }}
        >
          Clear
        </button>
        {lastExecutionTrace && (
          <button className="secondary" onClick={() => setTracePopup(lastExecutionTrace)}>
            View execution trace
          </button>
        )}
      </div>

      {rules.length > 0 && (
        <div className="card" style={{ marginTop: 20 }}>
          <h4>Rule execution results</h4>
          <table className="table">
            <thead>
              <tr>
                <th>
                  <input
                    type="checkbox"
                    checked={activeRules.length > 0 && activeRules.every((rule) => selectedRuleIds.includes(rule.id))}
                    onChange={toggleSelectAll}
                    aria-label="Select all active rules"
                  />
                </th>
                <th>Rule</th>
                <th>Status</th>
                <th>Decision</th>
                <th>Message</th>
                <th>Trace</th>
              </tr>
            </thead>
            <tbody>
              {activeRules.map((rule) => {
                const result = ruleRunResults.find((item) => item.ruleId === rule.id);
                const status = result ? (result.success ? 'Success' : 'Failure') : 'Not run';
                const decision = result?.decision ?? '—';
                const message = result?.message ?? 'Not executed yet';
                const checked = selectedRuleIds.includes(rule.id);

                return (
                  <tr key={rule.id}>
                    <td>
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() => toggleRuleSelection(rule.id)}
                        aria-label={`Select ${rule.ruleName}`}
                      />
                    </td>
                    <td>{rule.ruleName}</td>
                    <td>
                      <span style={{
                        padding: '4px 8px',
                        borderRadius: 999,
                        background: status === 'Success' ? '#d1fae5' : status === 'Failure' ? '#fee2e2' : '#e5e7eb',
                        color: status === 'Success' ? '#065f46' : status === 'Failure' ? '#991b1b' : '#374151',
                        fontWeight: 600,
                      }}>
                        {status}
                      </span>
                    </td>
                    <td>{decision}</td>
                    <td>{message}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {tracePopup && (
        <div style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(17, 24, 39, 0.6)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000,
          padding: 20,
        }}>
          <div className="card" style={{ width: 'min(1100px, 90vw)', maxHeight: '80vh', overflow: 'auto', margin: 0 }}>
            <div className="inline" style={{ justifyContent: 'space-between', marginBottom: 12 }}>
              <h3 style={{ margin: 0 }}>Execution Trace</h3>
              <button className="secondary" onClick={() => setTracePopup(null)}>Close</button>
            </div>

            <div className="card" style={{ margin: '0 0 20px', padding: 12 }}>
              <h4>API call details</h4>
              <div><strong>Endpoint:</strong> {tracePopup.apiName}</div>
            </div>

            <div style={{ marginTop: 20 }}>
              {tracePopup.calls?.map((call) => (
                <div key={call.callId} className="card" style={{ margin: '0 0 20px', padding: 12 }}>
                  <h4>{call.apiName}</h4>

                  <div style={{ marginBottom: 12 }}>
                    <strong>Request payload for this rule execution:</strong>
                    <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', margin: 8, background: '#f8fafc', padding: 12 }}>
                      {prettyJson(call.requestPayload || '')}
                    </pre>
                  </div>

                  <div style={{ marginBottom: 12 }}>
                    <strong>Exact JSON sent to GoRules:</strong>
                    <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', margin: 8, background: '#f8fafc', padding: 12 }}>
                      {prettyJson(call.goRulesPayload || '')}
                    </pre>
                  </div>

                  <div style={{ marginBottom: 12 }}>
                    <strong>Raw GoRules engine result (first match output):</strong>
                    <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', margin: 8, background: '#f8fafc', padding: 12 }}>
                      {prettyJson(call.goRulesResponse || '')}
                    </pre>
                  </div>

                  <div>
                    <strong>Final batch result returned to the app (all selected rules):</strong>
                    <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', margin: 8, background: '#f8fafc', padding: 12 }}>
                      {prettyJson(call.finalBatchResult || '')}
                    </pre>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );

  return (
    <div className="app-shell">
      <header className="topbar">
        <div>
          <h2 style={{ margin: 0 }}>GoRules Rule Studio</h2>
          <div className="muted">GoRules ZEN Engine mini POC</div>
        </div>

        <nav className="nav">
          <button className={page === 'dashboard' ? 'active' : ''} onClick={() => setPage('dashboard')}>Dashboard</button>
          <button className={page === 'rules' ? 'active' : ''} onClick={() => setPage('rules')}>Rule List</button>
          <button className={page === 'builder' ? 'active' : ''} onClick={() => setPage('builder')}>Rule Builder</button>
          <button className={page === 'execute' ? 'active' : ''} onClick={() => setPage('execute')}>Execute Rule</button>
        </nav>
      </header>

      {page === 'dashboard' && renderDashboard}
      {page === 'rules' && renderRules}
      {page === 'builder' && renderBuilder}
      {page === 'execute' && renderExecute}
    </div>
  );
}

export default App;
