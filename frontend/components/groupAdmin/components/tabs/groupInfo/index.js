import {useRef, useState} from "react";
import GroupIcon from "../../../../groupIcon";
import Description from "./description";
import ActionButton from "../../../../actionButton";
import {setGroupDescription, setGroupIcon, renameGroup} from "../../../../../services/groups";
import groupAdminStore from "../../../stores/groupAdminStore";
import authentication from "../../../../../stores/authentication";

const GroupInfo = props => {
  const store = groupAdminStore.useContainer();
  const auth = authentication.useContainer();
  const description = useRef(props.info.description);
  const fileRef = useRef(null);
  const [newName, setNewName] = useState('');
  const [nameError, setNameError] = useState(null);
  const [nameSuccess, setNameSuccess] = useState(null);
  const [nameSaving, setNameSaving] = useState(false);

  const isOwner = props.info.owner && auth.userId && props.info.owner.userId === auth.userId;

  return <div className='row mt-4'>
    <div className='col-12'>
      <h3>Group Information</h3>
    </div>
    <div className='col-4'>
      <GroupIcon id={props.groupId} />
      <div className='mt-4'>
        <input type='file' ref={fileRef} />
      </div>
    </div>
    <div className='col-8'>
      <Description setDescription={(v) => {
        description.current = v;
      }} description={description} />
    </div>
    <div className='col-12 mt-4'>
      <ActionButton label='Save' onClick={() => {
        let promises = [];
        if (fileRef.current && fileRef.current.files && fileRef.current.files.length) {
          promises.push(setGroupIcon({
            groupId: props.groupId,
            icon: fileRef.current.files[0],
          }));
        }
        if (description.current !== props.info.description) {
          promises.push(setGroupDescription({
            groupId: props.groupId,
            description: description.current,
          }));
        }

        Promise.all(promises).then(() => {
          window.location.reload();
        }).catch(e => {
          store.setFeedback('Error saving changes: ' + e.message);
        })
      }} />
    </div>

    {isOwner && <div className='col-12 mt-4'>
      <hr />
      <h4>Change Group Name</h4>
      <p className='text-muted mb-1'>Current name: <strong>{props.info.name}</strong></p>
      <p className='text-muted mb-2' style={{fontSize: '0.85em'}}>Costs 100 Robux. Maximum 1 change per month.</p>
      <div className='d-flex align-items-center gap-2'>
        <input
          type='text'
          maxLength={50}
          placeholder='New group name'
          value={newName}
          onChange={e => setNewName(e.currentTarget.value)}
          style={{width: 250}}
        />
        <ActionButton label={nameSaving ? 'Saving...' : 'Save Name (100 Robux)'} onClick={() => {
          if (!newName.trim()) return;
          setNameError(null);
          setNameSuccess(null);
          setNameSaving(true);
          renameGroup({groupId: props.groupId, name: newName.trim()}).then(() => {
            setNameSuccess('Group name changed successfully. Reloading...');
            setTimeout(() => window.location.reload(), 1500);
          }).catch(e => {
            setNameError(e.message || 'Failed to rename group');
            setNameSaving(false);
          });
        }} />
      </div>
      {nameError && <p className='text-danger mt-2'>{nameError}</p>}
      {nameSuccess && <p className='text-success mt-2'>{nameSuccess}</p>}
    </div>}
  </div>
}

export default GroupInfo;